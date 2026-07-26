using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Quest
{
    // Loads QuestCatalogSO during the boot sequence (IBootPreloader) and keeps it for scene consumers.
    public sealed class QuestCatalogProvider : IQuestCatalogProvider, IBootPreloader
    {
        private readonly QuestCatalogReference _reference;   // RegisterInstance at root
        public QuestCatalogSO Catalog { get; private set; }
        public string DisplayName => "Loading quests...";

        public QuestCatalogProvider(QuestCatalogReference reference) => _reference = reference;

        public async UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct)
        {
            if (_reference == null || !_reference.RuntimeKeyIsValid())
            {
                Debug.LogError("[QuestCatalogProvider] QuestCatalogReference is unassigned or its key is invalid.");
                return;
            }

            try
            {
                Catalog = await loader.LoadTrackerAsync(_reference);
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestCatalogProvider] Failed to load QuestCatalog: {e.Message}");
            }
        }
    }
}