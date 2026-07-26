using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Loading
{
    public interface IAssetLoader
    {
        UniTask<T> LoadTrackerAsync<T>(AssetReferenceT<T> reference) where T : Object;
    }
}