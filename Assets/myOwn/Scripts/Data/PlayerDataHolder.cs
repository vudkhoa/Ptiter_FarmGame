using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Farm;
using Core.Module.Map;
using Core.Module.Time;
using Core.Module.Storage;
using Core.Module.Quest;
using Core.Module.Quest.Cooking;
using Core.Module.Currency;
using Core.Module.Tutorial;
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
    public sealed class PlayerDataHolder :
        IService,
        IAsyncStartable,
        IStorageService,
        IFarmSaveSource,
        IMapSaveSource,
        IDailyQuestRepository,
        IProgressQuestRepository,
        ICookingJobRepository,
        ICurrencyRepository,
        ITutorialRepository,
        IDisposable
    {
        private readonly IPublisher<PlayerDataLoadedPayload> _loadedPublisher;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IDisposable _cheatSubscription;
        private readonly IPublisher<InventoryChangedPayload> _inventoryChangedPublisher;
        private readonly IPublisher<CurrencyBalanceSetRequestedPayload>
            _currencyBalanceSetPublisher;
        private PlayerData _data;

        public PlayerData Data => _data;
        public bool IsLoaded => _data != null;

        /// <summary>IFarmSaveSource: FarmService reads this itself when it is constructed.</summary>
        public List<FarmSlotSaveData> FarmSlots => _data?.FarmSlots;

        /// <summary>IMapSaveSource: MapService mutates this list and IStorageService persists it.</summary>
        public List<MapPlacementSaveData> MapPlacements => _data?.MapPlacements;

        public void SaveMap() => Save();

        /// <summary>True when Load() found no save file and fell back to a default PlayerData.</summary>
        public bool IsNewlyCreated { get; private set; }

        #region IStorageService Implementation
        public int Coins
        {
            get => _data != null ? _data.Coins : 0;
            set
            {
                if (_data == null) return;
                int nextBalance = Math.Max(0, value);
                if (_data.Coins == nextBalance) return;
                _currencyBalanceSetPublisher.Publish(
                    new CurrencyBalanceSetRequestedPayload(
                        $"storage-balance:{Guid.NewGuid():N}",
                        nextBalance,
                        "legacy-storage"));
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
            IPublisher<CurrencyBalanceSetRequestedPayload>
                currencyBalanceSetPublisher)
        {
            _loadedPublisher = loadedPublisher;
            _timeProvider = timeProvider;
            _inventoryChangedPublisher = inventoryChangedPublisher;
            _currencyBalanceSetPublisher = currencyBalanceSetPublisher;

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
            NotifyCurrencyReloaded("player-data-loaded");
        }

        public UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken)
        {
            return UniTask.WaitUntil(() => IsLoaded, cancellationToken: cancellationToken);
        }

        public DailyQuestSaveData LoadDailyQuest()
        {
            return Clone(_data?.DailyQuest);
        }

        public ProgressQuestSaveData LoadProgressQuest()
        {
            return Clone(_data?.QuestProgress);
        }

        #region ICookingJobRepository
        public CookingJobSaveData LoadActiveCookingJob()
        {
            return Clone(_data?.ActiveCookingJob);
        }

        public bool SaveActiveCookingJob(CookingJobSaveData job)
        {
            if (_data == null) return false;

            CookingJobSaveData previous = Clone(_data.ActiveCookingJob);
            _data.ActiveCookingJob = Clone(job);
            if (SaveImmediate()) return true;

            _data.ActiveCookingJob = previous;
            return false;
        }

        public bool TryCommitCookingCompletion(
            CookingCompletedPayload payload)
        {
            if (_data == null ||
                string.IsNullOrWhiteSpace(payload.TransactionId) ||
                string.IsNullOrWhiteSpace(payload.OutputItemId) ||
                payload.Amount <= 0)
                return false;

            _data.GrantedCookingCompletionTransactions ??=
                new List<string>();
            bool alreadyCommitted =
                _data.GrantedCookingCompletionTransactions.Contains(
                    payload.TransactionId);
            CookingJobSaveData previousJob =
                Clone(_data.ActiveCookingJob);

            if (alreadyCommitted)
            {
                if (previousJob != null && string.Equals(
                        previousJob.transactionId,
                        payload.TransactionId,
                        StringComparison.Ordinal))
                {
                    _data.ActiveCookingJob = null;
                    if (!SaveImmediate())
                    {
                        _data.ActiveCookingJob = previousJob;
                        return false;
                    }
                }

                return true;
            }

            if (previousJob == null || !string.Equals(
                    previousJob.transactionId,
                    payload.TransactionId,
                    StringComparison.Ordinal))
                return false;

            if (!string.Equals(
                    previousJob.recipeId,
                    payload.RecipeId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    previousJob.outputItemId,
                    payload.OutputItemId,
                    StringComparison.Ordinal) ||
                previousJob.outputAmount != payload.Amount ||
                previousJob.quantity != payload.Quantity)
                return false;

            _data.AddItem(previousJob.outputItemId, previousJob.outputAmount);
            _data.GrantedCookingCompletionTransactions.Add(
                payload.TransactionId);
            _data.ActiveCookingJob = null;

            if (SaveImmediate())
            {
                int newAmount =
                    _data.GetItemCount(previousJob.outputItemId);
                _inventoryChangedPublisher.Publish(
                    new InventoryChangedPayload(
                        previousJob.outputItemId,
                        newAmount,
                        previousJob.outputAmount));
                return true;
            }

            _data.RemoveItem(
                previousJob.outputItemId,
                previousJob.outputAmount);
            _data.GrantedCookingCompletionTransactions.Remove(
                payload.TransactionId);
            _data.ActiveCookingJob = previousJob;
            return false;
        }
        #endregion

        public UniTask<bool> SaveProgressQuestAsync(
            ProgressQuestSaveData data,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_data == null || data == null) return UniTask.FromResult(false);

            ProgressQuestSaveData previous = _data.QuestProgress;
            _data.QuestProgress = Clone(data);
            if (SaveImmediate()) return UniTask.FromResult(true);

            _data.QuestProgress = previous;
            return UniTask.FromResult(false);
        }

        public IReadOnlyList<PendingQuestRewardSaveData> LoadPendingQuestRewards()
        {
            var result = new List<PendingQuestRewardSaveData>();
            if (_data?.PendingQuestRewards == null) return result;
            for (int i = 0; i < _data.PendingQuestRewards.Count; i++)
                result.Add(Clone(_data.PendingQuestRewards[i]));
            return result;
        }

        public UniTask<bool> SaveDailyQuestAsync(
            DailyQuestSaveData data,
            bool immediate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_data == null) return UniTask.FromResult(false);

            _data.DailyQuest = Clone(data);
            if (!immediate)
            {
                Save();
                return UniTask.FromResult(true);
            }

            return UniTask.FromResult(SaveImmediate());
        }

        public UniTask<bool> StageQuestRewardAsync(
            DailyQuestSaveData data,
            PendingQuestRewardSaveData pendingReward,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_data == null || pendingReward == null ||
                string.IsNullOrWhiteSpace(pendingReward.transactionId))
                return UniTask.FromResult(false);

            _data.DailyQuest = Clone(data);
            _data.PendingQuestRewards ??= new List<PendingQuestRewardSaveData>();
            bool exists = _data.PendingQuestRewards.Exists(
                reward => reward != null &&
                          reward.transactionId == pendingReward.transactionId);
            if (!exists)
                _data.PendingQuestRewards.Add(Clone(pendingReward));

            return UniTask.FromResult(SaveImmediate());
        }

        public UniTask<bool> CompletePendingQuestRewardAsync(
            string transactionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_data == null || string.IsNullOrWhiteSpace(transactionId))
                return UniTask.FromResult(false);

            _data.PendingQuestRewards ??= new List<PendingQuestRewardSaveData>();
            int pendingIndex = _data.PendingQuestRewards.FindIndex(
                reward => reward != null && reward.transactionId == transactionId);
            if (pendingIndex < 0)
                return UniTask.FromResult(
                    _data.GrantedQuestRewardTransactions != null &&
                    _data.GrantedQuestRewardTransactions.Contains(transactionId));

            PendingQuestRewardSaveData pending =
                _data.PendingQuestRewards[pendingIndex];
            _data.PendingQuestRewards.RemoveAt(pendingIndex);

            if (SaveImmediate()) return UniTask.FromResult(true);
            _data.PendingQuestRewards.Insert(pendingIndex, pending);
            return UniTask.FromResult(false);
        }

        #region ICurrencyRepository
        public int Balance => _data != null ? _data.Coins : 0;

        public CurrencyRepositoryMutationResult ApplyDelta(
            string transactionId,
            int delta,
            bool trackTransaction)
        {
            if (_data == null)
            {
                return new CurrencyRepositoryMutationResult(
                    false, false, 0, 0, "PlayerData is not ready.");
            }

            _data.GrantedQuestRewardTransactions ??= new List<string>();
            bool alreadyApplied =
                trackTransaction &&
                !string.IsNullOrWhiteSpace(transactionId) &&
                _data.GrantedQuestRewardTransactions.Contains(transactionId);
            int previousBalance = _data.Coins;
            if (alreadyApplied)
            {
                return new CurrencyRepositoryMutationResult(
                    true,
                    true,
                    previousBalance,
                    previousBalance,
                    null);
            }

            long candidate = (long)previousBalance + delta;
            if (candidate < 0)
            {
                return new CurrencyRepositoryMutationResult(
                    false,
                    false,
                    previousBalance,
                    previousBalance,
                    "Insufficient coins.");
            }
            if (candidate > int.MaxValue)
            {
                return new CurrencyRepositoryMutationResult(
                    false,
                    false,
                    previousBalance,
                    previousBalance,
                    "Coin balance exceeds the supported range.");
            }

            _data.Coins = (int)candidate;
            bool addedTransaction =
                trackTransaction &&
                !string.IsNullOrWhiteSpace(transactionId);
            if (addedTransaction)
                _data.GrantedQuestRewardTransactions.Add(transactionId);

            if (SaveImmediate())
            {
                return new CurrencyRepositoryMutationResult(
                    true,
                    false,
                    previousBalance,
                    _data.Coins,
                    null);
            }

            _data.Coins = previousBalance;
            if (addedTransaction)
                _data.GrantedQuestRewardTransactions.Remove(transactionId);
            return new CurrencyRepositoryMutationResult(
                false,
                false,
                previousBalance,
                previousBalance,
                "Failed to persist currency transaction.");
        }

        public CurrencyRepositoryMutationResult SetBalance(int balance)
        {
            if (_data == null)
            {
                return new CurrencyRepositoryMutationResult(
                    false, false, 0, 0, "PlayerData is not ready.");
            }

            int previousBalance = _data.Coins;
            int nextBalance = Math.Max(0, balance);
            if (previousBalance == nextBalance)
            {
                return new CurrencyRepositoryMutationResult(
                    true, false, previousBalance, nextBalance, null);
            }

            _data.Coins = nextBalance;
            if (SaveImmediate())
            {
                return new CurrencyRepositoryMutationResult(
                    true, false, previousBalance, nextBalance, null);
            }

            _data.Coins = previousBalance;
            return new CurrencyRepositoryMutationResult(
                false,
                false,
                previousBalance,
                previousBalance,
                "Failed to persist the new coin balance.");
        }
        #endregion

        #region ITutorialRepository
        /// <summary>ITutorialRepository: distinguishes a fresh install from a save that predates the tutorial.</summary>
        public bool IsNewPlayer => IsNewlyCreated;

        public TutorialSaveData LoadTutorial()
        {
            if (_data == null) return null;

            _data.Tutorial ??= new TutorialSaveData();
            return Clone(_data.Tutorial);
        }

        public bool SaveTutorial(TutorialSaveData data)
        {
            if (_data == null || data == null) return false;

            TutorialSaveData previous = _data.Tutorial;
            _data.Tutorial = Clone(data);

            // Immediate, not throttled: a step credited in memory but lost to a crash would
            // replay the whole beat on the next launch.
            if (SaveImmediate()) return true;

            _data.Tutorial = previous;
            return false;
        }
        #endregion

        private static T Clone<T>(T value) where T : class
        {
            if (value == null) return default;
            string json = JsonUtility.ToJson(value, prettyPrint: false);
            return JsonUtility.FromJson<T>(json);
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
            long revision = PlayerDataSaveLoad.ReserveSaveRevision();
            ThrottledSaveAsync(revision, _saveCts.Token).Forget();
        }

        private async UniTaskVoid ThrottledSaveAsync(long revision, CancellationToken ct)
        {
            try
            {
                // Wait 1s; a newer save request cancels this one and restarts the countdown.
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);

                _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;

                // Serialize a stable snapshot on the main thread, then perform only IO off-thread.
                string json = JsonUtility.ToJson(_data, prettyPrint: false);
                await UniTask.RunOnThreadPool(() =>
                {
                    PlayerDataSaveLoad.SaveJson(json, revision);
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

        public bool SaveImmediate()
        {
            if (_data == null) return false;

            if (_saveCts != null)
            {
                _saveCts.Cancel();
                _saveCts.Dispose();
                _saveCts = null;
            }

            _data.LastSaveUtcTicks = _timeProvider.UtcNow.Ticks;
            string json = JsonUtility.ToJson(_data, prettyPrint: false);
            return PlayerDataSaveLoad.SaveJson(
                json,
                PlayerDataSaveLoad.ReserveSaveRevision());
        }

        public void Reset()
        {
            _data = new PlayerData();
            // Wiping an account must hit disk right away, not through the throttle.
            SaveImmediate();
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(true));
            NotifyCurrencyReloaded("player-data-reset");
        }

        private void NotifyCurrencyReloaded(string reason)
        {
            if (_data == null) return;
            _currencyBalanceSetPublisher.Publish(
                new CurrencyBalanceSetRequestedPayload(
                    $"currency-reload:{Guid.NewGuid():N}",
                    _data.Coins,
                    reason));
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
