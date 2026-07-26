# Loading Flow — Kiến trúc (tóm tắt)

> Mục tiêu: hiểu nhanh boot flow hiện tại. Chi tiết code xem `AddressablesLoadingSystem_Tasks.md`.

## 1. Ý tưởng chính

- **`LoadingService` (POCO, module Loading)** chạy 5 phase boot, không biết gì về Map/Farm/Quest.
- Mỗi module cần preload tự nộp một **`IBootPreloader`** — VContainer gom tất cả vào `IReadOnlyList<IBootPreloader>` và LoadingService duyệt qua.
- Asset load qua **`IAssetLoader.LoadTrackerAsync`** → handle được gom vào registry → **release tập trung** khi thoát app (`Dispose → ReleaseAll`).
- Tiến độ bắn qua **MessagePipe** (`LoadingProgressPayload`) → `LoadingScreenView` vẽ.

```
Loading  (tầng thấp — không ref module khác)
  ILoadingService · IAssetLoader · IBootPreloader
  LoadingService · LoadingConfigSO · LoadingPhase · LoadingProgressPayload

Map ──▶ Loading      ObjectCatalog   : IObjectCatalog, IBootPreloader   (dict ID→prefab)
Farm ──▶ Loading     FarmDatabaseProvider : IFarmDatabaseProvider, IBootPreloader
Quest ──▶ Loading    QuestCatalogProvider : IQuestCatalogProvider, IBootPreloader
MyOwn (app) ──▶ tất cả
```

## 2. Trình tự runtime

```
Scene Preloading
 └─ RootLifetimeScope.Awake
      ├─ base.Awake() → Build container (TRƯỚC DontDestroyOnLoad để
      │    RegisterComponentInHierarchy tìm được object trong scene)
      │    • RegisterLoadingModule (LoadingService + broker progress)
      │    • Register各module: ObjectCatalog / FarmDatabaseProvider / QuestCatalogProvider
      │      (AsImplementedInterfaces → lộ IBootPreloader)
      │    • RegisterInstance: ObjectDatabaseSO, FarmDatabaseReference, QuestCatalogReference
      │    • RegisterComponentInHierarchy: PreloadingFlow, LoadingScreenView
      └─ DontDestroyOnLoad(gameObject)

 └─ PreloadingFlow.Start  [inject ILoadingService]
      └─ await RunBootSequenceAsync(ct):
           1 Initialize      Addressables.InitializeAsync            (progress 0→5)
           2 CheckCatalog    CheckForCatalogUpdates/UpdateCatalogs   (5→10, local = no-op)
           3 Download        stub — local, chưa có remote            (nhảy 55)
           4 PreloadContent  foreach IBootPreloader → PreloadAsync   (55→95)
           5 Complete        progress 100
      └─ SceneManager.LoadSceneAsync("MapScene")

Scene MapScene
 └─ MapSceneBootstrap.Start (KHÔNG auto-inject — chạy trước khi scope của nó build)
      ├─ root.Container.Resolve<IFarmDatabaseProvider>().Database
      ├─ root.Container.Resolve<IQuestCatalogProvider>().Catalog
      ├─ null → FAIL-FAST (chặn vào game, tránh ghi đè save rỗng)
      ├─ Enqueue(RegisterInstance farm+quest) → GameLifetimeScope.Build()
      └─ bật _gameplayRoot (scene gate — con chạy Awake sau khi đã inject)

App quit → root scope dispose → LoadingService.Dispose → ReleaseAll (nhả mọi handle)
```

## 3. Quy tắc cần nhớ

| Quy tắc | Lý do |
|---|---|
| Preloader nhận `IAssetLoader` **qua tham số** `PreloadAsync(loader, ct)`, không inject | tránh vòng DI với LoadingService |
| Catalog/Provider + data SO đăng ký ở **ROOT**, không phải Game scope | preloader chạy lúc boot, trước khi MapScene tồn tại; cha không resolve được từ con |
| `base.Awake()` trước `DontDestroyOnLoad` trong RootLifetimeScope | RegisterComponentInHierarchy tìm theo scene của scope |
| 1 entry lỗi trong preload → `LogError + continue`, không kill boot | thiếu 1 vật ≠ chết game |
| FarmDatabase/QuestCatalog null → fail-fast, không dùng DB rỗng | DB rỗng + save/load = nguy cơ ghi đè save tốt |
| `MapService`: metadata từ `ObjectDatabaseSO` (inject), prefab từ `IObjectCatalog.TryGet(ID)` | data tĩnh vs asset runtime — 2 vai khác nhau |

## 4. Thêm module mới cần preload

1. Viết `XxxProvider : IXxxProvider, IBootPreloader` (load qua `loader.LoadTrackerAsync`).
2. Installer của module: `Register<XxxProvider>(Singleton).AsImplementedInterfaces().AsSelf()` ở ROOT.
3. Nếu cần asset ref/SO → `[SerializeField]` + `RegisterInstance` ở RootLifetimeScope.
4. Xong — LoadingService tự gom, không sửa gì trong module Loading.
