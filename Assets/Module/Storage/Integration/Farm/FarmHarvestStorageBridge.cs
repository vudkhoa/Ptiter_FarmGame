using System;
using Core.Module.Farm;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Storage.Integration.Farm
{
    /// <summary>
    /// Applies public Farm harvest events to Storage without coupling FarmService
    /// to inventory mutation.
    /// </summary>
    public sealed class FarmHarvestStorageBridge : IStartable, IDisposable
    {
        private readonly IStorageService _storage;
        private readonly IDisposable _subscription;

        public FarmHarvestStorageBridge(
            IStorageService storage,
            ISubscriber<FarmEntityHarvestedPayload> harvestedSubscriber)
        {
            _storage = storage;
            _subscription = harvestedSubscriber.Subscribe(OnHarvested);
        }

        public void Start()
        {
            // Subscription is established by construction.
        }

        private void OnHarvested(FarmEntityHarvestedPayload payload)
        {
            if (payload.Outputs == null) return;

            for (int i = 0; i < payload.Outputs.Length; i++)
            {
                OutputReward output = payload.Outputs[i];
                if (output.item == null || output.amount <= 0) continue;

                string itemId = output.item.ItemId;
                if (string.IsNullOrWhiteSpace(itemId)) continue;
                _storage.AddItem(itemId, output.amount);
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
