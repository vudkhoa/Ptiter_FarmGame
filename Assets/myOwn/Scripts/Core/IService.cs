using System.Threading;
using Cysharp.Threading.Tasks;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Marker interface cho mọi service trong harness.
    /// Service implements thêm <see cref="VContainer.Unity.IAsyncStartable"/> nếu cần init async sau khi container build.
    /// </summary>
    /// <remarks>
    /// Vì sao tách interface riêng thay vì dùng thẳng IAsyncStartable?
    /// - Để filter bằng AsImplementedInterfaces() khi register: chỉ resolve type implements IService.
    /// - Document semantic "đây là service domain", không phải startable bất kỳ.
    /// </remarks>
    public interface IService
    {
        /// <summary>
        /// Optional explicit init. VContainer tự gọi nếu service implement IAsyncStartable.StartAsync.
        /// Override method này khi muốn init thủ công (ví dụ trong test).
        /// </summary>
        UniTask InitializeAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
