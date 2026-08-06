using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Core.Module.Quest
{
    public sealed class ProgressQuestBootstrapper : IAsyncStartable
    {
        private readonly IProgressQuestService _service;

        public ProgressQuestBootstrapper(IProgressQuestService service)
        {
            _service = service;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            _service.EnsureInitializedAsync(cancellation).Forget();
            return UniTask.CompletedTask;
        }
    }
}
