using System.Threading;
using Cysharp.Threading.Tasks;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Marker cho service domain. Tách khỏi IAsyncStartable để document semantic + filter resolve.
    /// </summary>
    public interface IService
    {
        /// <summary>Optional explicit init (gọi tay từ test hoặc cloud-restore flow).</summary>
        UniTask InitializeAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
