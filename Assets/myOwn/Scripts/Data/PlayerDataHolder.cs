using System;
using System.Threading;
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
    /// Wrap PlayerData runtime instance, expose Load/Save/Reset.
    /// POCO Singleton; auto-loads trong StartAsync khi container build.
    /// Implements IStorageService to serve as the unified warehouse contract.
    /// </summary>
    public sealed class PlayerDataHolder : IService, IAsyncStartable, IStorageService
    {
        private readonly IPublisher<PlayerDataLoadedPayload> _loadedPublisher;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IDisposable _cheatSubscription;
        private readonly IPublisher<InventoryChangedPayload> _inventoryChangedPublisher;
        private readonly IObjectResolver _resolver;
        private PlayerData _data;

        public PlayerData Data => _data;

        /// <summary>True khi Load() KHÔNG tìm thấy save file → tạo PlayerData mặc định.</summary>
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
            IPublisher<InventoryChangedPayload> inventoryChangedPublisher,
            IObjectResolver resolver)
        {
            _loadedPublisher = loadedPublisher;
            _timeProvider = timeProvider;
            _inventoryChangedPublisher = inventoryChangedPublisher;
            _resolver = resolver;

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

            // Khởi tạo FarmService bằng dữ liệu Save vừa load
            if (_resolver != null)
            {
                try
                {
                    var farmService = _resolver.Resolve<Core.Module.Farm.IFarmService>();
                    if (farmService != null)
                    {
                        farmService.Initialize(_data.FarmSlots, _data.LastSaveUtcTicks);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlayerDataHolder] Bỏ qua khởi tạo FarmService (thường do chạy UnitTest hoặc khởi động sớm): {e.Message}");
                }
            }

            _loadedPublisher.Publish(new PlayerDataLoadedPayload(IsNewlyCreated));
        }

        private CancellationTokenSource _saveCts;

        public void Save()
        {
            if (_data == null) return;

            // Hủy tác vụ lưu đang chờ trước đó để bắt đầu đếm ngược lại (Throttling)
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
                // Trì hoãn 1 giây (nếu có click mới trong 1s này, tác vụ sẽ bị hủy và đếm lại từ đầu)
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);

                _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;

                // Sao chép tham chiếu dữ liệu để ghi bất đồng bộ trên ThreadPool, tránh chặn main thread
                PlayerData saveCopy = _data;
                await UniTask.RunOnThreadPool(() =>
                {
                    PlayerDataSaveLoad.Save(saveCopy);
                }, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Bị hủy khi có yêu cầu Save tiếp theo trước 1s, đây là hoạt động bình thường của Throttling
#if UNITY_EDITOR
                Debug.LogWarning("[PlayerDataHolder] Save operation canceled because a newer save request was initiated.");
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
            SaveImmediate(); // Khi reset tài khoản, ghi đè lập tức
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(true));
        }

        public void Dispose()
        {
            SaveImmediate(); // Đảm bảo mọi thay đổi đang chờ trong RAM được ghi xuống ổ đĩa trước khi hủy đối tượng
            _cheatSubscription?.Dispose();
        }
    }
}
