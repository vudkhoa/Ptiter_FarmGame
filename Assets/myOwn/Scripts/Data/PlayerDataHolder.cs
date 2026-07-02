using System;
using System.Threading;
using Core.Module.Time;
using Core.Module.Farm;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Wrap PlayerData runtime instance, expose Load/Save/Reset.
    /// POCO Singleton; auto-loads trong StartAsync khi container build.
    /// Implements IFarmInventoryProvider to decouple Farm module.
    /// </summary>
    public sealed class PlayerDataHolder : IService, IAsyncStartable, IFarmInventoryProvider
    {
        private readonly IPublisher<PlayerDataLoadedPayload> _loadedPublisher;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IDisposable _cheatSubscription;
        private PlayerData _data;

        public PlayerData Data => _data;

        /// <summary>True khi Load() KHÔNG tìm thấy save file → tạo PlayerData mặc định.</summary>
        public bool IsNewlyCreated { get; private set; }

        #region IFarmInventoryProvider Implementation
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

        public void AddItem(string itemId, int amount) => _data?.AddItem(itemId, amount);

        public bool RemoveItem(string itemId, int amount) => _data != null && _data.RemoveItem(itemId, amount);
        #endregion

        public PlayerDataHolder(
            IPublisher<PlayerDataLoadedPayload> loadedPublisher,
            IServerTimeProvider timeProvider,
            ISubscriber<ClockManipulationDetectedPayload> cheatSub)
        {
            _loadedPublisher = loadedPublisher;
            _timeProvider = timeProvider;

            // Subscribe to cheat events to lock game production
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

        /// <summary>Re-init explicit (idempotent) — dùng sau cloud restore hoặc trong test.</summary>
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
            Debug.Log($"[PlayerDataHolder] Loaded. IsNewlyCreated={IsNewlyCreated}, SaveVersion={_data.SaveVersion}, Coins={_data.Coins}");
        }

        public void Save()
        {
            if (_data == null) return;
            _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;
            PlayerDataSaveLoad.Save(_data);
        }

        public void Reset()
        {
            _data = new PlayerData();
            Save();
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(true));
        }

        public void Dispose()
        {
            _cheatSubscription?.Dispose();
        }
    }
}
