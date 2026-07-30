using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Core.Module.Quest
{
    public sealed class DailyQuestBootstrapper : IAsyncStartable
    {
        private readonly IDailyQuestService _dailyQuestService;

        public DailyQuestBootstrapper(IDailyQuestService dailyQuestService)
        {
            _dailyQuestService = dailyQuestService;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            // Daily may stay locked while WorldTimeAPI is unavailable; gameplay startup must not block.
            _dailyQuestService.EnsureInitializedAsync(cancellation).Forget();
            return UniTask.CompletedTask;
        }
    }
}
