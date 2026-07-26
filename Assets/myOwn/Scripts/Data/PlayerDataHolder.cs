using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Farm;
using Core.Module.Time;
using Core.Module.Storage;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Owns the runtime PlayerData instance and exposes Load/Save/Reset.
    /// Loads itself in StartAsync when the container builds; serves as the single storage contract.
    /// </summary>
    public sealed class PlayerDataHolder : IService, IAsyncStartable, IStorageService, IFarmSaveSource
    {
        private readonly IPublisher<PlayerDataLoadedPayload> _loadedPublisher;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IDisposable _cheatSubscription;
        private readonly IPublisher<InventoryChangedPayload> _inventoryChangedPublisher;
        private PlayerData _data;

        public PlayerData Data => _data;

        /// <summary>IFarmSaveSource: FarmService reads this itself when it is constructed.</summary>
        public List<FarmSlotSaveData> FarmSlots => _data?.FarmSlots;

        /// <summary>True when Load() found no save file and fell back to a default PlayerData.</summary>
        public bool IsNewlyCreated { get; private set; }

        #region IStorageService Implementation
        public int Coins
        {
            get => _data != null ? _data.Coins : 0;
            set
            {
                if (_data != null) _data.Coins = value;
            }
        }

        public bool IsCheatDetected
        {
            get => _data != null && _data.IsCheatDetected;
            set
            {
                if (_data != null) _data.IsCheatDetected = value;
            }
        }

        public long LastSaveUtcTicks => _data != null ? _data.LastSaveUtcTicks : 0;

        public int GetItemCount(string itemId) => _data != null ? _data.GetItemCount(itemId) : 0;

        public void AddItem(string itemId, int amount)
        {
            if (_data == null) return;
            _data.AddItem(itemId, amount);
            int newAmt = _data.GetItemCount(itemId);
            _inventoryChangedPublisher.Publish(new InventoryChangedPayload(itemId, newAmt, amount));
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (_data == null) return false;
            bool success = _data.RemoveItem(itemId, amount);
            if (success)
            {
                int newAmt = _data.GetItemCount(itemId);
                _inventoryChangedPublisher.Publish(new InventoryChangedPayload(itemId, newAmt, -amount));
            }
            return success;
        }
        #endregion

        public PlayerDataHolder(
            IPublisher<PlayerDataLoadedPayload> loadedPublisher,
            IServerTimeProvider timeProvider,
            ISubscriber<ClockManipulationDetectedPayload> cheatSub,
            IPublisher<InventoryChangedPayload> inventoryChangedPublisher)
        {
            _loadedPublisher = loadedPublisher;
            _timeProvider = timeProvider;
            _inventoryChangedPublisher = inventoryChangedPublisher;

            // Listen for clock tampering so production can be locked down.
            _cheatSubscription = cheatSub.Subscribe(OnCheatDetected);
        }

        private void OnCheatDetected(ClockManipulationDetectedPayload payload)
        {
            if (_data != null && !_data.IsCheatDetected)
            {
                _data.IsCheatDetected = true;
                Save();
                Debug.LogError($"[PlayerDataHolder] Cheat detected! Expected: {payload.ExpectedUtc}, Actual: {payload.ActualUtc}, Drift: {payload.Drift.TotalSeconds}s. Save locked.");
            }
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            Load();
            return UniTask.CompletedTask;
        }

        /// <summary>Idempotent re-init, used after a cloud restore or inside tests.</summary>
        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Load()
        {
            var loaded = PlayerDataSaveLoad.Load();
            IsNewlyCreated = loaded == null;
            _data = loaded ?? new PlayerData();

            _loadedPublisher.Publish(new PlayerDataLoadedPayload(IsNewlyCreated));
        }

        private CancellationTokenSource _saveCts;

        public void Save()
        {
            if (_data == null) return;

            // Cancel the pending save so the 1s countdown restarts (throttling).
            if (_saveCts != null)
            {
                _saveCts.Cancel();
                _saveCts.Dispose();
            }

            _saveCts = new CancellationTokenSource();
            ThrottledSaveAsync(_saveCts.Token).Forget();
        }

        private async UniTaskVoid ThrottledSaveAsync(CancellationToken ct)
        {
            try
            {
                // Wait 1s; a newer save request cancels this one and restarts the countdown.
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);

                _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;

                // Hold the reference locally so the write can run off the main thread.
                PlayerData saveCopy = _data;
                await UniTask.RunOnThreadPool(() =>
                {
                    PlayerDataSaveLoad.Save(saveCopy);
                }, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Normal throttling behaviour, not a failure: a newer save request replaced this one.
#if UNITY_EDITOR
                Debug.Log("[PlayerDataHolder] Save skipped - a newer save request replaced it.");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataHolder] Throttled save failed: {e.Message}");
            }
        }

        public void SaveImmediate()
        {
            if (_data == null) return;

            if (_saveCts != null)
            {
                _saveCts.Cancel();
                _saveCts.Dispose();
                _saveCts = null;
            }

            _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;
            PlayerDataSaveLoad.Save(_data);
        }

        public void Reset()
        {
            _data = new PlayerData();
            // Wiping an account must hit disk right away, not through the throttle.
            SaveImmediate();
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(true));
        }

        public void Dispose()
        {
            // Flush pending in-memory changes to disk before this object goes away.
            SaveImmediate();

            _cheatSubscription?.Dispose();

            if (_saveCts != null)
            {
                _saveCts.Cancel();
                _saveCts.Dispose();
                _saveCts = null;
            }
        }
    }
}
