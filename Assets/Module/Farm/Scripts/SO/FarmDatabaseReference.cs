using System;
using UnityEngine.AddressableAssets;

namespace Core.Module.Farm
{
    /// <summary>
    /// AssetReference khoá cứng theo FarmDatabaseSO — Inspector chỉ cho kéo đúng loại asset.
    /// Dùng subclass cụ thể thay vì AssetReferenceT&lt;&gt; trần để chắc chắn serialize được.
    /// </summary>
    [Serializable]
    public sealed class FarmDatabaseReference : AssetReferenceT<FarmDatabaseSO>
    {
        public FarmDatabaseReference(string guid) : base(guid) { }
    }
}
