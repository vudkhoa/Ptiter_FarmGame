using Cysharp.Threading.Tasks;
using MessagePipe;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Module.Loading
{
    public class LoadingService : ILoadingService, IAssetLoader, IDisposable
    {
        private readonly IReadOnlyList<IBootPreloader> _preloads;
        private readonly IPublisher<LoadingProgressPayload> _progressPub;
        private readonly List<AsyncOperationHandle> _handles = new();
        private bool _initialized = false;

        #region VContainer
        public LoadingService(
            IReadOnlyList<IBootPreloader> preloads,
            IPublisher<LoadingProgressPayload> progressPub)
        { 
            _preloads = preloads;
            _progressPub = progressPub;
        }

        public void Dispose() => ReleaseAll();
        #endregion

        #region main flow
        public async UniTask RunBootSequenceAsync(CancellationToken ct = default)
        {
            // Phase 1: Init
            if (!_initialized)
            {
                Report(LoadingPhase.Initialize, 0, "Initialize ...");
                await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);
                _initialized = true;
            }
            Report(LoadingPhase.Initialize, 5, "Done Initialize");

            // Phase 2: Catalog Check
            Report(LoadingPhase.CheckCatalog, 5, "Catalog Checking...");
            var catalogs = await Addressables.CheckForCatalogUpdates().ToUniTask(cancellationToken: ct);
            if (catalogs != null && catalogs.Count > 0)
                await Addressables.UpdateCatalogs().ToUniTask(cancellationToken: ct);
            Report(LoadingPhase.CheckCatalog, 10, "Catalog Ready");

            // Phase 3: Download -> Flow For Remote.
            Report(LoadingPhase.Download, 55, "Success Download");

            // Phase 3: Preloading Content
            int count = _preloads.Count;
            for (int i = 0; i < count; ++i)
            {
                ct.ThrowIfCancellationRequested();
                var p = _preloads[i];
                Report(LoadingPhase.PreloadContent, (count == 0 ? 0 : 55 + (int)(40f * i / count)), p.DisplayName);
                await p.PreloadAsync(this, ct); // this = loader.
            }

            Report(LoadingPhase.Complete, 100, "Complete Loading Game.");
        }

        public async UniTask<T> LoadTrackerAsync<T>(AssetReferenceT<T> reference) where T : UnityEngine.Object
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
        #endregion

        #region helper
        private void Report(LoadingPhase phase, int progress, string mess) 
            => _progressPub.Publish(new LoadingProgressPayload(phase, progress, mess));
        #endregion

    }
}