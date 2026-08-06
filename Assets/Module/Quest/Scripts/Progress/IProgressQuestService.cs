using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public interface IProgressQuestService
    {
        bool IsReady { get; }
        ProgressQuestViewState GetViewState();
        UniTask EnsureInitializedAsync(
            CancellationToken cancellationToken = default);
        UniTask<bool> ClaimMilestoneAsync(
            string milestoneId,
            CancellationToken cancellationToken = default);
    }
}
