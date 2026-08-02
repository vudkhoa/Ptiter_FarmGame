using System;
using UnityEngine.AddressableAssets;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Khoá kiểu asset trong Inspector. Subclass cụ thể để chắc chắn serialize được.
    /// </summary>
    [Serializable]
    public sealed class CutsceneCatalogReference : AssetReferenceT<CutsceneCatalogSO>
    {
        public CutsceneCatalogReference(string guid) : base(guid) { }
    }
}
