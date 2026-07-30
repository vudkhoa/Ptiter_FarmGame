using Cysharp.Threading.Tasks;
using System.Threading;

namespace Core.Module.Loading
{
    public interface IBootPreloader
    {
        string DisplayName { get; }
        UniTask PreloadAsync(IAssetLoader loader, CancellationToken ct);
    }
}