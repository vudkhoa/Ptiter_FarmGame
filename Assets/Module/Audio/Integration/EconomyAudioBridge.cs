using System;
using Core.Module.Currency;
using Core.Module.Quest;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public sealed class EconomyAudioBridge : IStartable, IDisposable
    {
        private readonly IAudioService _audio;
        private readonly AudioCatalogSO _catalog;
        private readonly AudioFeedbackService _feedback;
        private readonly IDisposable _subscriptions;

        public EconomyAudioBridge(
            IAudioService audio,
            AudioCatalogSO catalog,
            AudioFeedbackService feedback,
            ISubscriber<CurrencyCreditedPayload> creditedSubscriber,
            ISubscriber<CurrencyTransactionProcessedPayload> processedSubscriber,
            ISubscriber<QuestRewardGrantedPayload> questRewardSubscriber,
            ISubscriber<ProgressRewardClaimedPayload> progressRewardSubscriber)
        {
            _audio = audio;
            _catalog = catalog;
            _feedback = feedback;

            var bag = DisposableBag.CreateBuilder();
            creditedSubscriber.Subscribe(_ => _audio.PlaySfx(_catalog.Coin)).AddTo(bag);
            processedSubscriber.Subscribe(OnCurrencyProcessed).AddTo(bag);
            questRewardSubscriber.Subscribe(OnQuestReward).AddTo(bag);
            progressRewardSubscriber.Subscribe(_ =>
                _audio.PlaySfx(_catalog.ClaimReward)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Start() { }

        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        private void OnCurrencyProcessed(CurrencyTransactionProcessedPayload payload)
        {
            if (!payload.Success)
                PlayError();
        }

        private void OnQuestReward(QuestRewardGrantedPayload payload)
        {
            if (!payload.ReconciledAtStartup)
                _audio.PlaySfx(_catalog.ClaimReward);
        }

        internal void PlayError() =>
            _feedback.PlayError();
    }
}
