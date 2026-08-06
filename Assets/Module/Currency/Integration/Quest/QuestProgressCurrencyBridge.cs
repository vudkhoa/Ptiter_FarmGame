using System;
using Core.Module.Quest;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Currency.Integration.Quest
{
    public sealed class QuestProgressCurrencyBridge : IStartable, IDisposable
    {
        private readonly IPublisher<ProgressCoinsEarnedPayload> _progressPublisher;
        private readonly IDisposable _creditedSubscription;

        public QuestProgressCurrencyBridge(
            ISubscriber<CurrencyCreditedPayload> creditedSubscriber,
            IPublisher<ProgressCoinsEarnedPayload> progressPublisher)
        {
            _progressPublisher = progressPublisher;
            _creditedSubscription = creditedSubscriber.Subscribe(OnCredited);
        }

        public void Start() { }

        private void OnCredited(CurrencyCreditedPayload payload)
        {
            if (payload.Amount <= 0) return;
            _progressPublisher.Publish(new ProgressCoinsEarnedPayload(
                payload.TransactionId, payload.Amount));
        }

        public void Dispose()
        {
            _creditedSubscription?.Dispose();
        }
    }
}
