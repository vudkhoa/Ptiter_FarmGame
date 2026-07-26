using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Map
{
    /// <summary>
    /// Preload toàn bộ prefab vật thể lúc boot (IBootPreloader) và cho MapService tra theo ID.
    /// Chỉ nạp asset — KHÔNG giữ state (cây lớn/thu hoạch nằm ở hệ entity riêng).
    /// </summary>
    public sealed class ObjectCatalog : IObjectCatalog, IBootPreloader
    {
        private readonly ObjectDatabaseSO _database;              // RegisterInstance ở root
        private readonly Dictionary<int, GameObject> _dict = new();

        public string DisplayName => "Loading objects...";

        public ObjectCatalog(ObjectDatabaseSO database) => _database = database;

        public bool TryGet(int id, out GameObject prefab) => _dict.TryGetValue(id, out prefab);

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            for (int i = 0; i < _database.Objects.Count; ++i)
            {
                ct.ThrowIfCancellationRequested();
                ObjectData dt = _database.Objects[i];
                if (dt.Prefab == null || !dt.Prefab.RuntimeKeyIsValid())
                {
                    Debug.LogError($"[ObjectCatalog] ID {dt.ID} has no valid AssetRef - skipped.");
                    continue;
                }
                try
                {
                    GameObject gO = await loader.LoadTrackerAsync(dt.Prefab);
                    if (gO != null)
                    {
                        _dict[dt.ID] = gO;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PreloadAsync] Loading Failed: {e.Message}");
                }
            }
        }
    }
}