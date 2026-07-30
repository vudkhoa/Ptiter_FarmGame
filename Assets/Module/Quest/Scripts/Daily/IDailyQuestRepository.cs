using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public interface IDailyQuestRepository
    {
        bool IsLoaded { get; }
        UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken);
        DailyQuestSaveData LoadDailyQuest();
        IReadOnlyList<PendingQuestRewardSaveData> LoadPendingQuestRewards();
        UniTask<bool> SaveDailyQuestAsync(
            DailyQuestSaveData data,
            bool immediate,
            CancellationToken cancellationToken);
        UniTask<bool> StageQuestRewardAsync(
            DailyQuestSaveData data,
            PendingQuestRewardSaveData pendingReward,
            CancellationToken cancellationToken);
    }

    public interface IQuestRewardService
    {
        UniTask<QuestRewardGrantResult> GrantPendingCoinsAsync(
            string transactionId,
            CancellationToken cancellationToken);
    }

    public readonly struct QuestRewardGrantResult
    {
        public readonly bool Success;
        public readonly bool AlreadyGranted;
        public readonly int Coins;
        public readonly string Error;

        public QuestRewardGrantResult(bool success, bool alreadyGranted, int coins, string error)
        {
            Success = success;
            AlreadyGranted = alreadyGranted;
            Coins = coins;
            Error = error;
        }
    }
}
