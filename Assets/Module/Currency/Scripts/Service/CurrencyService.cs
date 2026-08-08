using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Currency
{
    public sealed class CurrencyService :
        ICurrencyService,
        IAsyncStartable,
        IDisposable
    {
        private readonly ICurrencyRepository _repository;
        private readonly IPublisher<CurrencyTransactionProcessedPayload> _processedPublisher;
        private readonly IPublisher<CurrencyChangedPayload> _changedPublisher;
        private readonly IPublisher<CurrencyCreditedPayload> _creditedPublisher;
        private readonly IDisposable _transactionSubscription;
        private readonly IDisposable _setBalanceSubscription;

        public int Balance => _repository.IsLoaded ? _repository.Balance : 0;

        public CurrencyService(
            ICurrencyRepository repository,
            ISubscriber<CurrencyTransactionRequestedPayload> transactionSubscriber,
            ISubscriber<CurrencyBalanceSetRequestedPayload> setBalanceSubscriber,
            IPublisher<CurrencyTransactionProcessedPayload> processedPublisher,
            IPublisher<CurrencyChangedPayload> changedPublisher,
            IPublisher<CurrencyCreditedPayload> creditedPublisher)
        {
            _repository = repository;
            _processedPublisher = processedPublisher;
            _changedPublisher = changedPublisher;
            _creditedPublisher = creditedPublisher;
            _transactionSubscription =
                transactionSubscriber.Subscribe(OnTransactionRequested);
            _setBalanceSubscription =
                setBalanceSubscriber.Subscribe(OnBalanceSetRequested);
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _repository.WaitUntilLoadedAsync(cancellation);
            int balance = _repository.Balance;
            _changedPublisher.Publish(new CurrencyChangedPayload(
                "currency-initialized",
                balance,
                balance,
                0,
                "initialize"));
        }

        public bool AddCoins(int amount, string reason = null)
        {
            if (amount <= 0) return false;
            return ProcessTransaction(new CurrencyTransactionRequestedPayload(
                $"currency-credit:{Guid.NewGuid():N}",
                CurrencyTransactionType.Credit,
                amount,
                reason ?? "direct-credit")).Success;
        }

        public bool TrySpend(int amount, string reason = null)
        {
            if (amount <= 0) return false;
            return ProcessTransaction(new CurrencyTransactionRequestedPayload(
                $"currency-debit:{Guid.NewGuid():N}",
                CurrencyTransactionType.Debit,
                amount,
                reason ?? "direct-debit")).Success;
        }

        private void OnTransactionRequested(
            CurrencyTransactionRequestedPayload payload)
        {
            CurrencyTransactionProcessedPayload result =
                ProcessTransaction(payload);
            _processedPublisher.Publish(result);
        }

        private CurrencyTransactionProcessedPayload ProcessTransaction(
            CurrencyTransactionRequestedPayload payload)
        {
            if (!_repository.IsLoaded)
                return Failed(payload.TransactionId, "PlayerData is not ready.");
            if (string.IsNullOrWhiteSpace(payload.TransactionId))
                return Failed(payload.TransactionId, "Transaction ID is required.");
            if (payload.Amount <= 0)
                return Failed(payload.TransactionId, "Amount must be greater than zero.");
            if (payload.Type != CurrencyTransactionType.Credit &&
                payload.Type != CurrencyTransactionType.Debit)
                return Failed(payload.TransactionId, "Transaction type is invalid.");

            int delta = payload.Type == CurrencyTransactionType.Credit
                ? payload.Amount
                : -payload.Amount;
            CurrencyRepositoryMutationResult mutation =
                _repository.ApplyDelta(payload.TransactionId, delta, true);

            var result = new CurrencyTransactionProcessedPayload(
                payload.TransactionId,
                mutation.Success,
                mutation.AlreadyApplied,
                mutation.PreviousBalance,
                mutation.CurrentBalance,
                mutation.CurrentBalance - mutation.PreviousBalance,
                mutation.Error);

            if (mutation.Success)
            {
                int appliedDelta = mutation.CurrentBalance - mutation.PreviousBalance;
                _changedPublisher.Publish(new CurrencyChangedPayload(
                    payload.TransactionId,
                    mutation.PreviousBalance,
                    mutation.CurrentBalance,
                    appliedDelta,
                    payload.Reason));
                if (!mutation.AlreadyApplied &&
                    payload.Type == CurrencyTransactionType.Credit &&
                    appliedDelta > 0)
                {
                    _creditedPublisher.Publish(new CurrencyCreditedPayload(
                        payload.TransactionId,
                        appliedDelta,
                        payload.Reason));
                }
            }
            return result;
        }

        private void OnBalanceSetRequested(
            CurrencyBalanceSetRequestedPayload payload)
        {
            if (!_repository.IsLoaded)
            {
                _processedPublisher.Publish(Failed(
                    payload.TransactionId, "PlayerData is not ready."));
                return;
            }
            if (string.IsNullOrWhiteSpace(payload.TransactionId))
            {
                _processedPublisher.Publish(Failed(
                    payload.TransactionId, "Transaction ID is required."));
                return;
            }

            CurrencyRepositoryMutationResult mutation =
                _repository.SetBalance(Math.Max(0, payload.Balance));
            int delta = mutation.CurrentBalance - mutation.PreviousBalance;
            _processedPublisher.Publish(new CurrencyTransactionProcessedPayload(
                payload.TransactionId,
                mutation.Success,
                mutation.AlreadyApplied,
                mutation.PreviousBalance,
                mutation.CurrentBalance,
                delta,
                mutation.Error));
            if (mutation.Success)
            {
                _changedPublisher.Publish(new CurrencyChangedPayload(
                    payload.TransactionId,
                    mutation.PreviousBalance,
                    mutation.CurrentBalance,
                    delta,
                    payload.Reason));
            }
        }

        private CurrencyTransactionProcessedPayload Failed(
            string transactionId,
            string error)
        {
            int balance = Balance;
            return new CurrencyTransactionProcessedPayload(
                transactionId,
                false,
                false,
                balance,
                balance,
                0,
                error);
        }

        public void Dispose()
        {
            _transactionSubscription?.Dispose();
            _setBalanceSubscription?.Dispose();
        }
    }
}
