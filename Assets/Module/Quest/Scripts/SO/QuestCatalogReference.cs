using System;
using UnityEngine.AddressableAssets;

namespace Core.Module.Quest
{
    // AssetReference locked to QuestCatalogSO (mirror of FarmDatabaseReference).
    [Serializable]
    public sealed class QuestCatalogReference : AssetReferenceT<QuestCatalogSO>
    {
        public QuestCatalogReference(string guid) : base(guid) { }
    }
}