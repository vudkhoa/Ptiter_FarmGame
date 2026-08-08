using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public interface IProgressQuestRepository
    {
        bool IsLoaded { get; }
        UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken);
        ProgressQuestSaveData LoadProgressQuest();
        UniTask<bool> SaveProgressQuestAsync(
            ProgressQuestSaveData data,
            CancellationToken cancellationToken);
    }
}
