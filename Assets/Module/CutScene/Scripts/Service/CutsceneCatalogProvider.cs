using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Cutscene
{
    // Load CutsceneCatalogSO trong boot sequence (IBootPreloader), giữ lại cho scene consumer.
    public sealed class CutsceneCatalogProvider : ICutsceneCatalogProvider, IBootPreloader
    {
        private readonly CutsceneCatalogReference _reference;   // RegisterInstance ở root

        public CutsceneCatalogSO Catalog { get; private set; }
        public string DisplayName => "Loading cutscenes...";

        public CutsceneCatalogProvider(CutsceneCatalogReference reference) => _reference = reference;

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            // Thiếu cutscene không phải lý do chặn gameplay -> chỉ warning, khác Farm/Quest.
            if (_reference == null || !_reference.RuntimeKeyIsValid())
            {
                Debug.LogWarning("[CutsceneCatalogProvider] CutsceneCatalogReference chưa gán - bỏ qua.");
                return;
            }

            try
            {
                Catalog = await loader.LoadTrackerAsync(_reference);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CutsceneCatalogProvider] Load CutsceneCatalog thất bại: {e.Message}");
            }
        }
    }
}
