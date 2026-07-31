using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Quest;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace Core.Module.Currency.Integration.Quest
{
    public sealed class QuestCurrencyEventBridge :
        IQuestRewardService,
        IDisposable
    {
        private readonly IDailyQuestRepository _questRepository;
        private readonly IPublisher<CurrencyTransactionRequestedPayload>
            _currencyRequestPublisher;
        private readonly IDisposable _processedSubscription;
        private readonly Dictionary<
            string,
            UniTaskCompletionSource<CurrencyTransactionProcessedPayload>>
            _pendingResponses =
                new Dictionary<
                    string,
                    UniTaskCompletionSource<CurrencyTransactionProcessedPayload>>(
                    StringComparer.Ordinal);

        public QuestCurrencyEventBridge(
            IDailyQuestRepository questRepository,
            IPublisher<CurrencyTransactionRequestedPayload>
                currencyRequestPublisher,
            ISubscriber<CurrencyTransactionProcessedPayload>
                processedSubscriber)
        {
            _questRepository = questRepository;
            _currencyRequestPublisher = currencyRequestPublisher;
            _processedSubscription =
                processedSubscriber.Subscribe(OnCurrencyTransactionProcessed);
        }

        public async UniTask<QuestRewardGrantResult> GrantPendingCoinsAsync(
            string transactionId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return new QuestRewardGrantResult(
                    false, false, 0, "Transaction ID is required.");
            }

            PendingQuestRewardSaveData pending = FindPending(transactionId);
            if (pending == null)
            {
                return new QuestRewardGrantResult(
                    false, false, 0, "Pending reward was not found.");
            }

            if (pending.coins <= 0)
            {
                bool zeroRewardCompleted =
                    await _questRepository.CompletePendingQuestRewardAsync(
                        transactionId,
                        cancellationToken);
                return zeroRewardCompleted
                    ? new QuestRewardGrantResult(true, false, 0, null)
                    : new QuestRewardGrantResult(
                        false,
                        false,
                        0,
                        "The zero-value reward could not be finalized.");
            }

            var responseSource =
                new UniTaskCompletionSource<CurrencyTransactionProcessedPayload>();
            if (_pendingResponses.ContainsKey(transactionId))
            {
                return new QuestRewardGrantResult(
                    false, false, 0, "Transaction is already being processed.");
            }
            _pendingResponses.Add(transactionId, responseSource);

            CurrencyTransactionProcessedPayload response;
            try
            {
                _currencyRequestPublisher.Publish(
                    new CurrencyTransactionRequestedPayload(
                        transactionId,
                        CurrencyTransactionType.Credit,
                        pending.coins,
                        $"quest:{pending.kind}:{pending.sourceId}"));
                response = await responseSource.Task.AttachExternalCancellation(
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
                _pendingResponses.Remove(transactionId);
            }

            if (!response.Success)
            {
                return new QuestRewardGrantResult(
                    false,
                    response.AlreadyApplied,
                    0,
                    response.Error ?? "Currency transaction failed.");
            }

            bool completed =
                await _questRepository.CompletePendingQuestRewardAsync(
                    transactionId,
                    cancellationToken);
            if (!completed)
            {
                return new QuestRewardGrantResult(
                    false,
                    response.AlreadyApplied,
                    0,
                    "Currency was granted but the pending reward could not be finalized.");
            }

            return new QuestRewardGrantResult(
                true,
                response.AlreadyApplied,
                pending.coins,
                null);
        }

        private PendingQuestRewardSaveData FindPending(string transactionId)
        {
            IReadOnlyList<PendingQuestRewardSaveData> pending =
                _questRepository.LoadPendingQuestRewards();
            for (int i = 0; i < pending.Count; i++)
            {
                PendingQuestRewardSaveData candidate = pending[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.transactionId,
                        transactionId,
                        StringComparison.Ordinal))
                    return candidate;
            }
            return null;
        }

        private void OnCurrencyTransactionProcessed(
            CurrencyTransactionProcessedPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.TransactionId)) return;
            if (_pendingResponses.TryGetValue(
                    payload.TransactionId,
                    out UniTaskCompletionSource<
                        CurrencyTransactionProcessedPayload> response))
                response.TrySetResult(payload);
        }

        public void Dispose()
        {
            _processedSubscription?.Dispose();
            _pendingResponses.Clear();
        }
    }
}
