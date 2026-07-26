# Addressables Flow — Project hiện tại (tóm tắt)

> Trạng thái: **local-only, thiết kế remote-ready**. Bật remote sau chỉ cần đổi profile + viết phase Download, không đổi kiến trúc.

## 1. Asset nào đi qua Addressables

| Asset | Reference type | Ai load | Khi nào |
|---|---|---|---|
| Prefab vật thể (bench, cây, đất…) | `AssetReferenceGameObject` trong `ObjectData` (`ObjectDatabaseSO`) | `ObjectCatalog.PreloadAsync` | Boot (phase PreloadContent) |
| `FarmDatabaseSO` | `FarmDatabaseReference : AssetReferenceT<FarmDatabaseSO>` | `FarmDatabaseProvider` | Boot |
| `QuestCatalogSO` | `QuestCatalogReference : AssetReferenceT<QuestCatalogSO>` | `QuestCatalogProvider` | Boot |

- Scene (`Preloading`, `MapScene`) **không** qua Addressables — vẫn Build Settings + `SceneManager`.
- Groups hiện tại: local (`Local.BuildPath` / `Local.LoadPath`), đã tách theo module (Farm / Map / Quest).

## 2. Nguyên tắc "một asset = một nguồn"

- Prefab đã là Addressable thì **không** hard-ref (`GameObject` field) ở bất kỳ SO/scene nào khác → tránh duplicate bundle + nạp RAM sớm.
- `ObjectDatabaseSO` chỉ giữ **AssetReference** (địa chỉ) + metadata (ID, Size, name). Prefab thật nằm trong bundle, chỉ vào RAM khi preload.

## 3. Vòng đời load / release

```
AssetReference.LoadAssetAsync()
        │ trả AsyncOperationHandle ("vé giữ đồ", Addressables refcount qua vé này)
        ▼
LoadingService.LoadTrackerAsync(ref)
        ├─ _handles.Add(handle)      ← ghi vé vào registry NGAY (trước await)
        └─ await handle.ToUniTask()  → trả asset
        ...
App quit → Dispose() → ReleaseAll(): duyệt _handles → Addressables.Release(mỗi vé)
```

Quy tắc:
- **Mọi load đi qua `LoadTrackerAsync`** — không module nào tự gọi `Addressables.LoadAssetAsync` rồi tự quản handle.
- `AssetReferenceT.LoadAssetAsync()` **chỉ gọi 1 lần / reference** (reference cache handle bên trong; gọi lần 2 sẽ lỗi).
- Release handle **download** ≠ unload asset; release handle **load** mới nhả asset (khi refcount về 0).

## 4. Boot phases ↔ Addressables API

| Phase | API | Local hiện tại |
|---|---|---|
| Initialize | `Addressables.InitializeAsync()` | chạy 1 lần (guard `_initialized`) |
| CheckCatalog | `CheckForCatalogUpdates()` → `UpdateCatalogs()` | list rỗng → no-op |
| Download | `GetDownloadSizeAsync(labels)` → `DownloadDependenciesAsync` | **stub** — chưa cần vì local (size luôn 0) |
| PreloadContent | `LoadTrackerAsync` qua từng `IBootPreloader` | load prefab + FarmDB + QuestCatalog vào RAM |

## 5. Khi bật remote (việc tương lai)

1. Addressables Profiles: điền `Remote.LoadPath` = URL CDN; group cần patch → chuyển BuildPath/LoadPath sang Remote.
2. Viết thân phase Download trong `LoadingService`: `GetDownloadSizeAsync(_config.CriticalLabels)` → nếu >0 `DownloadDependenciesAsync` + publish progress (`Progress.Create<float>`); inject lại `LoadingConfigSO` vào constructor (hiện chưa dùng).
3. Gán label cho asset remote → điền vào `LoadingConfigSO.CriticalLabels`.
4. Build content (`Build → New Build`), upload `ServerData/` lên CDN.
5. Không sửa preloader/provider nào — flow giữ nguyên.

## 6. Lỗi hay gặp

| Triệu chứng | Nguyên nhân |
|---|---|
| `No such registration of type: XxxSO` | quên `RegisterInstance` SO/reference ở RootLifetimeScope |
| `TryGet` miss / không đặt được vật | `ObjectData.Prefab` chưa gán AssetRef, hoặc preload bị skip (xem LogError của ObjectCatalog) |
| "not preloaded - blocking gameplay" | FarmDatabase/QuestCatalog ref chưa gán ở root, hoặc asset chưa vào Addressable group |
| Exception khi load lần 2 cùng reference | gọi `LoadAssetAsync` lặp trên 1 AssetReference — chỉ load 1 lần |
| Leak asset sau nhiều session | load ngoài `LoadTrackerAsync` nên không vào registry |
