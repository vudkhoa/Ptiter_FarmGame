using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Quest
{
    public enum StarUnlockPurchaseState
    {
        Success = 0,
        AlreadyUnlocked = 1,
        InsufficientStars = 2,
        InvalidRequest = 3,
        SaveFailed = 4,
        Busy = 5
    }

    public readonly struct StarUnlockPurchaseResult
    {
        public readonly StarUnlockPurchaseState State;
        public readonly int RemainingStars;

        public bool Succeeded =>
            State == StarUnlockPurchaseState.Success ||
            State == StarUnlockPurchaseState.AlreadyUnlocked;

        public StarUnlockPurchaseResult(
            StarUnlockPurchaseState state,
            int remainingStars)
        {
            State = state;
            RemainingStars = remainingStars;
        }
    }

    public interface IStarWalletService
    {
        bool IsReady { get; }
        int Stars { get; }
        UniTask EnsureInitializedAsync(
            CancellationToken cancellationToken = default);
        bool IsStarUnlockPurchased(string unlockId);
        UniTask<StarUnlockPurchaseResult> TryPurchaseStarUnlockAsync(
            string unlockId,
            int cost,
            CancellationToken cancellationToken = default);
    }
}
