using System;
using UnityEngine.AddressableAssets;

namespace Core.Module.Farm
{
    /// <summary>
    /// AssetReference locked to FarmDatabaseSO so the Inspector only accepts the right asset type.
    /// A concrete subclass is used instead of a bare AssetReferenceT&lt;&gt; to guarantee serialization.
    /// </summary>
    [Serializable]
    public sealed class FarmDatabaseReference : AssetReferenceT<FarmDatabaseSO>
    {
        public FarmDatabaseReference(string guid) : base(guid) { }
    }
}
