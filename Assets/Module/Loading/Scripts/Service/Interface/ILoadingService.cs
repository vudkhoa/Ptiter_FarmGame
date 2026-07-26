using Cysharp.Threading.Tasks;
using System.Threading;

namespace Core.Module.Loading
{
    public interface ILoadingService
    {
        UniTask RunBootSequenceAsync(CancellationToken ct = default);
        void ReleaseAll();
    }
}