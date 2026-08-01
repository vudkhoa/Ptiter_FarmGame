using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Module.Currency
{
    public interface ICurrencyRepository
    {
        bool IsLoaded { get; }
        int Balance { get; }

        UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken);

        CurrencyRepositoryMutationResult ApplyDelta(
            string transactionId,
            int delta,
            bool trackTransaction);

        CurrencyRepositoryMutationResult SetBalance(int balance);
    }

    public readonly struct CurrencyRepositoryMutationResult
    {
        public readonly bool Success;
        public readonly bool AlreadyApplied;
        public readonly int PreviousBalance;
        public readonly int CurrentBalance;
        public readonly string Error;

        public CurrencyRepositoryMutationResult(
            bool success,
            bool alreadyApplied,
            int previousBalance,
            int currentBalance,
            string error)
        {
            Success = success;
            AlreadyApplied = alreadyApplied;
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            Error = error;
        }
    }
}
