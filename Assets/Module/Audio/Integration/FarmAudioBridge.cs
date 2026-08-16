using System;
using Core.Module.Farm;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public sealed class FarmAudioBridge : IStartable, IDisposable
    {
        private readonly IDisposable _subscriptions;

        public FarmAudioBridge(
            IAudioService audio,
            AudioCatalogSO catalog,
            ISubscriber<FarmEntityPlantedPayload> plantedSubscriber,
            ISubscriber<FarmEntityCaredPayload> caredSubscriber,
            ISubscriber<FarmEntityHarvestedPayload> harvestedSubscriber)
        {
            var bag = DisposableBag.CreateBuilder();
            plantedSubscriber.Subscribe(_ => audio.PlaySfx(catalog.Plant)).AddTo(bag);
            caredSubscriber.Subscribe(_ => audio.PlaySfx(catalog.Care)).AddTo(bag);
            harvestedSubscriber.Subscribe(_ => audio.PlaySfx(catalog.Harvest)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Start() { }

        public void Dispose() => _subscriptions?.Dispose();
    }
}
