using System;
using Core.Module.Map;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public sealed class MapAudioBridge : IStartable, IDisposable
    {
        private readonly IDisposable _subscription;

        public MapAudioBridge(
            IAudioService audio,
            AudioCatalogSO catalog,
            ISubscriber<MapFurnitureAddedPayload> furnitureAddedSubscriber)
        {
            _subscription = furnitureAddedSubscriber.Subscribe(payload =>
            {
                if (payload.AnimatePlacement)
                    audio.PlaySfx(catalog.PlaceObject);
            });
        }

        public void Start() { }

        public void Dispose() => _subscription?.Dispose();
    }
}
