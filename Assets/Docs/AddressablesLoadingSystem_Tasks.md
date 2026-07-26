# Addressables Loading — Task Breakdown + Scripts (code hoàn chỉnh)

> Cách B (mỗi module nộp `IBootPreloader`) · `LoadingService` = POCO · remote-ready, chạy local trước.
> Doc này để **học/ôn Addressables + UniTask** → code viết đầy đủ, chú thích tại chỗ. Legend: ⬜ · 🟡 · ✅ · ⛔ · **User = CORE**.

---

## 1. Task Breakdown

### Loading module
| # | Task | Owner | Status |
|---|------|-------|--------|
| L0 | asmdef Loading + ref: UniTask, **UniTask.Addressables**, Unity.Addressables, Unity.ResourceManager, VContainer, MessagePipe | User | ⬜ |
| L1 | `LoadingPhase` + `LoadingProgressPayload` | — | ✅ đã có |
| L2 | `ILoadingService` + `IAssetLoader` + `IBootPreloader` | Claude | ⬜ |
| L3 | `LoadingConfigSO` | Claude | ⬜ |
| L4 | `LoadingService` (POCO) | Claude | ⬜ |
| L5 | **CORE ôn tập: đọc + tự viết lại 4 phase** | **User** | ⬜ |
| L6 | `LoadingModuleInstaller` | Claude | ⬜ |

### Map / Farm / App
| # | Task | Owner | Status |
|---|------|-------|--------|
| M0 | asmdef Map + ref **Loading** | User | ⬜ |
| M1 | `ObjectDatabaseSO`: Prefab → `AssetReferenceGameObject` | Claude | ⬜ |
| M2 | Re-author `Objects` SO + label `"furniture"` | User | ⬜ |
| M3 | `IFurnitureCatalog` + `FurnitureCatalog` | Claude | ⬜ |
| M4 | **CORE: `MapService` dùng catalog** | **User** | ⬜ |
| M5 | Map installer: register `FurnitureCatalog` | Claude | ⬜ |
| F0 | asmdef Farm + ref **Loading** | User | ⬜ |
| F1 | `IFarmDatabaseProvider` + `FarmDatabaseProvider` | Claude | ⬜ |
| F2 | Farm installer: register `FarmDatabaseProvider` | Claude | ⬜ |
| A1 | `RootLifetimeScope`: RegisterLoadingModule + RegisterInstance config SO + RegisterComponentInHierarchy PreloadingFlow | Claude | ⬜ |
| A2 | `LoadingConfig` asset + group remote-ready + QuestData mồ côi | User | ⬜ |
| A3 | `PreloadingFlow` await boot | Claude | ⬜ |
| A4 | `MapSceneBootstrap` fail-fast | Claude | ⬜ |
| A5 | UI `LoadingScreen` + `LoadingScreenView` | User+Claude | ⬜ |
| A6 | Wire refs + Debug `[ContextMenu]` + review | Claude/Together | ⬜ |

---

## 2. Scripts (code hoàn chỉnh)

### `Loading/Scripts/Service/ILoadingService.cs`
```csharp
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Loading
{
    public interface ILoadingService
    {
        UniTask RunBootSequenceAsync(CancellationToken ct = default);
        void ReleaseAll();
    }
}
```

### `Loading/Scripts/Service/IAssetLoader.cs`
```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Loading
{
    // Preloader dùng interface này để load; handle được LoadingService gom vào registry để release tập trung.
    public interface IAssetLoader
    {
        UniTask<T> LoadTrackedAsync<T>(AssetReferenceT<T> reference) where T : Object;
    }
}
```

### `Loading/Scripts/Service/IBootPreloader.cs`
```csharp
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Loading
{
    // Mỗi module implements để nộp phần preload của mình. loader ĐI QUA THAM SỐ (tránh vòng DI).
    public interface IBootPreloader
    {
        string DisplayName { get; }
        UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct);
    }
}
```

### `Loading/Scripts/SO/LoadingConfigSO.cs`
```csharp
using UnityEngine;

namespace Core.Module.Loading
{
    [CreateAssetMenu(fileName = "LoadingConfig", menuName = "Data/Loading/Config")]
    public class LoadingConfigSO : ScriptableObject
    {
        [Tooltip("Label của các asset cần đảm bảo tải xong trước khi vào game.")]
        public string[] CriticalLabels;

        [Tooltip("Để trống khi chạy local. Điền URL CDN khi bật remote.")]
        public string RemotePathOverride;

        public int TimeoutSeconds = 30;
        public int RetryCount = 2;
    }
}
```

### `Loading/Scripts/Service/LoadingService.cs`
> Ôn tập: chú ý `handle.ToUniTask()` (cần asmdef **UniTask.Addressables**), `Progress.Create<float>` để nhận % download, và `Addressables.Release` khi xong.
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UObject = UnityEngine.Object;

namespace Core.Module.Loading
{
    public sealed class LoadingService : ILoadingService, IAssetLoader, IDisposable
    {
        private readonly IReadOnlyList<IBootPreloader> _preloaders;   // VContainer gom mọi IBootPreloader đã đăng ký
        private readonly IPublisher<LoadingProgressPayload> _progressPub;
        private readonly LoadingConfigSO _config;
        private readonly List<AsyncOperationHandle> _handles = new();  // registry để release tập trung
        private bool _initialized;

        public LoadingService(
            IReadOnlyList<IBootPreloader> preloaders,
            IPublisher<LoadingProgressPayload> progressPub,
            LoadingConfigSO config)
        {
            _preloaders = preloaders;
            _progressPub = progressPub;
            _config = config;
        }

        public async UniTask RunBootSequenceAsync(CancellationToken ct = default)
        {
            // ---- Phase 1: Initialize -------------------------------------------------
            if (!_initialized)
            {
                Report(LoadingPhase.Initialize, 0, "Khởi tạo...");
                await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);
                _initialized = true;
            }
            Report(LoadingPhase.Initialize, 5, "Khởi tạo xong");

            // ---- Phase 2: CheckCatalog (local: rỗng, no-op) --------------------------
            Report(LoadingPhase.CheckCatalog, 5, "Kiểm tra cập nhật...");
            var catalogs = await Addressables.CheckForCatalogUpdates().ToUniTask(cancellationToken: ct);
            if (catalogs != null && catalogs.Count > 0)
                await Addressables.UpdateCatalogs(catalogs).ToUniTask(cancellationToken: ct);
            Report(LoadingPhase.CheckCatalog, 10, "Catalog sẵn sàng");

            // ---- Phase 3: Download (local: size == 0 → skip) ------------------------
            if (_config.CriticalLabels != null && _config.CriticalLabels.Length > 0)
            {
                IEnumerable keys = _config.CriticalLabels;   // ép sang IEnumerable để chọn đúng overload
                long size = await Addressables.GetDownloadSizeAsync(keys).ToUniTask(cancellationToken: ct);
                if (size > 0)
                {
                    Report(LoadingPhase.Download, 10, $"Đang tải {size / (1024f * 1024f):0.0} MB...");
                    var dl = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, false);
                    // Progress.Create nhận % (0..1) realtime từ handle → map sang 10..55
                    await dl.ToUniTask(
                        Progress.Create<float>(pct => Report(LoadingPhase.Download, 10 + (int)(pct * 45), "Đang tải nội dung...")),
                        cancellationToken: ct);
                    Addressables.Release(dl);   // release handle download (KHÔNG unload asset, chỉ nhả op handle)
                }
            }
            Report(LoadingPhase.Download, 55, "Tải xong");

            // ---- Phase 4: PreloadContent (nạp asset vào RAM qua từng module) --------
            int count = _preloaders.Count;
            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var p = _preloaders[i];
                Report(LoadingPhase.PreloadContent, 55 + (count == 0 ? 0 : (int)(40f * i / count)), p.DisplayName);
                await p.PreloadAsync(this, ct);   // truyền `this` làm IAssetLoader
            }

            // ---- Phase 5: Complete ---------------------------------------------------
            Report(LoadingPhase.Complete, 100, "Sẵn sàng");
        }

        // Load 1 asset + ghi handle vào registry. Gọi 1 lần / reference (Addressables cache handle trong reference).
        public async UniTask<T> LoadTrackedAsync<T>(AssetReferenceT<T> reference) where T : UObject
        {
            AsyncOperationHandle<T> handle = reference.LoadAssetAsync();
            _handles.Add(handle);
            return await handle.ToUniTask();
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _handles.Count; i++)
                if (_handles[i].IsValid())
                    Addressables.Release(_handles[i]);
            _handles.Clear();
        }

        private void Report(LoadingPhase phase, int progress, string msg)
            => _progressPub.Publish(new LoadingProgressPayload(phase, progress, msg));

        public void Dispose() => ReleaseAll();   // VContainer gọi Dispose khi root scope hủy (thoát app)
    }
}
```

### `Loading/Scripts/Bootstrap/LoadingModuleInstaller.cs`
```csharp
using MessagePipe;
using VContainer;

namespace Core.Module.Loading
{
    public static class LoadingModuleInstaller
    {
        // Gọi ở ROOT scope. LoadingConfigSO được RegisterInstance ở RootLifetimeScope.
        public static IContainerBuilder RegisterLoadingModule(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<LoadingProgressPayload>(options);
            builder.Register<LoadingService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()   // ILoadingService + IAssetLoader + IDisposable
                   .AsSelf();
            return builder;
        }
    }
}
```

### `Map/Scripts/SO/ObjectDatabaseSO.cs`
```csharp
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Map
{
    [CreateAssetMenu(fileName = "Objects", menuName = "Data/Map/Objects")]
    public class ObjectDatabaseSO : ScriptableObject
    {
        public List<ObjectData> Objects;
    }

    [Serializable]
    public struct ObjectData
    {
        public string name;
        public int ID;
        public Vector2Int Size;
        public AssetReferenceGameObject AssetRef;   // was: public GameObject Prefab;
    }
}
```

### `Map/Scripts/Service/IFurnitureCatalog.cs`
```csharp
using UnityEngine;

namespace Core.Module.Map
{
    public interface IFurnitureCatalog
    {
        bool TryGet(int id, out GameObject prefab);
    }
}
```

### `Map/Scripts/Service/FurnitureCatalog.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Map
{
    public sealed class FurnitureCatalog : IFurnitureCatalog, IBootPreloader
    {
        private readonly ObjectDatabaseSO _database;                 // RegisterInstance ở root
        private readonly Dictionary<int, GameObject> _dict = new();

        public string DisplayName => "Đang tải nội thất...";
        public FurnitureCatalog(ObjectDatabaseSO database) => _database = database;

        public bool TryGet(int id, out GameObject prefab) => _dict.TryGetValue(id, out prefab);

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            var list = _database.Objects;
            for (int i = 0; i < list.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var entry = list[i];

                if (entry.AssetRef == null || !entry.AssetRef.RuntimeKeyIsValid())
                {
                    Debug.LogError($"[FurnitureCatalog] ID {entry.ID} ({entry.name}): AssetRef chưa gán/không hợp lệ — bỏ qua.");
                    continue;   // 1 vật lỗi không được làm chết cả boot
                }

                try
                {
                    GameObject prefab = await loader.LoadTrackedAsync(entry.AssetRef);
                    _dict[entry.ID] = prefab;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FurnitureCatalog] Load ID {entry.ID} lỗi: {e.Message}");
                }
            }
        }
    }
}
```

### `Map/Scripts/Service/MapService.cs` — phần sửa (đầy đủ)
```csharp
// 1) Field
private IFurnitureCatalog _catalog;

// 2) Construct — thêm tham số cuối
[Inject]
public void Construct(
    IPublisher<MapPlacementStartedPayload> pubStart,
    IPublisher<MapPreviewMovedPayload> pubMove,
    IPublisher<MapFurnitureAddedPayload> pubAdded,
    IPublisher<MapPlacementStoppedPayload> pubStop,
    IFurnitureCatalog catalog)
{
    _pubStart = pubStart;
    _pubMove = pubMove;
    _pubAdded = pubAdded;
    _pubStop = pubStop;
    _catalog = catalog;
}

// 3) StartPlacement — resolve prefab NGAY sau khi có data, trước khi đổi state
public void StartPlacement(int objectId)
{
    if (HasActivePlacement) StopPlacement();

    int idx = -1;
    for (int i = 0; i < _database.Objects.Count; ++i)
        if (_database.Objects[i].ID == objectId) { idx = i; break; }

    if (idx < 0) { Debug.LogError($"ObjectId {objectId} not found"); return; }

    var data = _database.Objects[idx];
    if (!_catalog.TryGet(data.ID, out var prefab))
    {
        Debug.LogError($"[MapService] Prefab ID {data.ID} chưa preload trong catalog.");
        return;
    }

    _currentObjectId = data.ID;
    _currentDbIndex = idx;
    _lastCell = new Vector3Int(int.MinValue, 0, 0);
    _pubStart.Publish(new MapPlacementStartedPayload(data.ID, prefab, data.Size));
}

// 4) AddFurniture — resolve prefab trước khi ghi grid
public bool AddFurniture(Vector3 worldHit)
{
    if (!HasActivePlacement) return false;

    var cell = WorldToCell(worldHit);
    var data = _database.Objects[_currentDbIndex];

    if (!_grid.CanPlaceObjectAt(cell, data.Size) || !IsTilemapPlacementValid(cell, data.Size)) return false;
    if (!_catalog.TryGet(data.ID, out var prefab))
    {
        Debug.LogError($"[MapService] Prefab ID {data.ID} chưa preload trong catalog.");
        return false;
    }

    _grid.AddObjectAt(cell, data.Size, data.ID, _changeCount);
    _changeCount++;
    var snapped = CellToWorld(cell);
    _pubAdded.Publish(new MapFurnitureAddedPayload(data.ID, prefab, snapped, cell, _changeCount));
    return true;
}
```

### `Map/Scripts/Bootstrap/MapModuleInstaller.cs` — thêm vào `RegisterMapModule`
```csharp
// FurnitureCatalog là service global (preload lúc boot) → đăng ký ở ROOT scope.
builder.Register<FurnitureCatalog>(Lifetime.Singleton)
       .AsImplementedInterfaces()   // IFurnitureCatalog + IBootPreloader
       .AsSelf();
```

### `Farm/Scripts/Service/IFarmDatabaseProvider.cs`
```csharp
namespace Core.Module.Farm
{
    public interface IFarmDatabaseProvider
    {
        FarmDatabaseSO Database { get; }
    }
}
```

### `Farm/Scripts/Service/FarmDatabaseProvider.cs`
```csharp
using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Farm
{
    public sealed class FarmDatabaseProvider : IFarmDatabaseProvider, IBootPreloader
    {
        private readonly FarmDatabaseReference _reference;   // RegisterInstance ở root
        public FarmDatabaseSO Database { get; private set; }
        public string DisplayName => "Đang tải dữ liệu nông trại...";

        public FarmDatabaseProvider(FarmDatabaseReference reference) => _reference = reference;

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            if (_reference == null || !_reference.RuntimeKeyIsValid())
            {
                Debug.LogError("[FarmDatabaseProvider] FarmDatabaseReference chưa gán/không hợp lệ.");
                return;   // Database == null → MapSceneBootstrap fail-fast
            }

            try
            {
                Database = await loader.LoadTrackedAsync(_reference);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FarmDatabaseProvider] Load FarmDatabase lỗi: {e.Message}");
            }
        }
    }
}
```

### `Farm/Scripts/Bootstrap/FarmModuleInstaller.cs` — thêm vào `RegisterFarmModule`
```csharp
builder.Register<FarmDatabaseProvider>(Lifetime.Singleton)
       .AsImplementedInterfaces()   // IFarmDatabaseProvider + IBootPreloader
       .AsSelf();
```

### `myOwn/Scripts/Bootstrap/RootLifetimeScope.cs` — thêm
```csharp
// Config asset cấp cho POCO preloader lúc boot (kéo vào Inspector của RootLifetimeScope)
[SerializeField] private Core.Module.Loading.LoadingConfigSO _loadingConfig;
[SerializeField] private Core.Module.Map.ObjectDatabaseSO _objectDatabase;
[SerializeField] private Core.Module.Farm.FarmDatabaseReference _farmDatabaseRef;

// ...trong Configure(), sau các RegisterXModule:
builder.RegisterLoadingModule(options);
builder.RegisterInstance(_loadingConfig);
builder.RegisterInstance(_objectDatabase);
builder.RegisterInstance(_farmDatabaseRef);

// PreloadingFlow ở cùng scene Preloading → đăng ký để được [Inject]
builder.RegisterComponentInHierarchy<PreloadingFlow>();
```

### `myOwn/Scripts/Bootstrap/PreloadingFlow.cs` — viết lại
```csharp
using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace MyOwn.ServiceHarness
{
    public sealed class PreloadingFlow : MonoBehaviour
    {
        private const string GameSceneName = "MapScene";
        private ILoadingService _loading;

        [Inject]
        public void Construct(ILoadingService loading) => _loading = loading;

        private void Start() => RunAsync(this.GetCancellationTokenOnDestroy()).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            try
            {
                await _loading.RunBootSequenceAsync(ct);                 // chờ boot xong
                await SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single)
                                  .ToUniTask(cancellationToken: ct);     // rồi mới mở scene
            }
            catch (OperationCanceledException) { /* thoát khi đang load — bình thường */ }
            catch (Exception e)
            {
                Debug.LogError($"[PreloadingFlow] Boot failed: {e}");
            }
        }
    }
}
```

### `Bootstrap/Scripts/MapSceneBootstrap.cs` — phần sửa (fail-fast)
> Ôn tập nuance: `MapSceneBootstrap` chạy TRƯỚC khi scope của nó build → không scope nào auto-inject nó. Lấy provider từ **root container** thủ công.
```csharp
private async UniTaskVoid BuildScopeAsync()
{
    if (_scope == null)
    {
        Debug.LogError("[MapSceneBootstrap] Scope field trống.");
        return;
    }
    if (_gameplayRoot != null && _gameplayRoot.activeSelf)
        Debug.LogWarning($"[MapSceneBootstrap] '{_gameplayRoot.name}' đang active — tắt đi.");

    // Lấy FarmDatabase đã preload từ root scope (global, DontDestroyOnLoad)
    var root = FindAnyObjectByType<RootLifetimeScope>();
    var provider = root.Container.Resolve<IFarmDatabaseProvider>();
    var database = provider.Database;

    if (database == null)   // FAIL-FAST: không build với DB rỗng (tránh ghi đè save rỗng)
    {
        Debug.LogError("[MapSceneBootstrap] FarmDatabase chưa preload — CHẶN vào gameplay.");
        return;
    }

    using (LifetimeScope.Enqueue(b => b.RegisterInstance(database)))
        _scope.Build();

    if (_gameplayRoot == null)
    {
        Debug.LogError("[MapSceneBootstrap] GameplayRoot trống.");
        return;
    }
    _gameplayRoot.SetActive(true);
}
// Xoá LoadFarmDatabaseAsync() và OnDestroy release — release đã tập trung ở LoadingService.
```

### `myOwn/.../LoadingScreenView.cs`
```csharp
using System;
using Core.Module.Loading;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MyOwn.ServiceHarness
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _bar;
        [SerializeField] private Text _label;
        private IDisposable _sub;

        [Inject]
        public void Construct(ISubscriber<LoadingProgressPayload> sub)
            => _sub = sub.Subscribe(OnProgress);

        private void OnProgress(LoadingProgressPayload p)
        {
            if (_bar != null) _bar.value = p.Progress / 100f;   // Progress = 0..100
            if (_label != null) _label.text = p.Message;
        }

        private void OnDestroy() => _sub?.Dispose();
    }
}
```

---

## 3. Ghi chú ôn tập (Addressables + UniTask)

- **`handle.ToUniTask()`**: chuyển `AsyncOperationHandle` → awaitable. Nằm trong asmdef **UniTask.Addressables** → Loading phải ref nó, không là không thấy extension.
- **`Progress.Create<float>(cb)`**: `Cysharp.Threading.Tasks.Progress` — nhận % realtime của handle (dùng cho download bar).
- **`InitializeAsync`**: Addressables tự init lần đầu, nhưng gọi tường minh để **gate** + bắt lỗi sớm.
- **`GetDownloadSizeAsync`**: local trả `0` → skip download. Chỉ remote mới >0.
- **`Addressables.Release`**: nhả *operation handle*. Release handle download ≠ unload asset. Release handle của `LoadAssetAsync` mới là nhả asset (khi refcount về 0).
- **`AssetReferenceT<T>.LoadAssetAsync()`**: cache handle trong chính reference → **gọi 1 lần/reference**; gọi lần 2 sẽ ném lỗi.
- **Progress**: Initialize 5 → CheckCatalog 10 → Download 10..55 → PreloadContent 55..95 → Complete 100 (đơn điệu tăng).
```
