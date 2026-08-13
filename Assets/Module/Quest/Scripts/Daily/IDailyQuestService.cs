using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public interface IDailyQuestService
    {
        bool IsReady { get; }
        DailyQuestViewState GetViewState();
        UniTask EnsureInitializedAsync(CancellationToken cancellationToken = default);
        UniTask<bool> ClaimTaskAsync(
            string runtimeId,
            CancellationToken cancellationToken = default);
        UniTask<bool> ClaimMilestoneAsync(
            string milestoneId,
            CancellationToken cancellationToken = default);
    }
}
