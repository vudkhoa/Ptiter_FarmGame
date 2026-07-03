using System;
using System.Collections.Generic;
using Core.Module.Time;
using Core.Module.Storage;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Farm
{
    public class FarmService : IFarmService, IDisposable
    {
        private readonly IServerTimeProvider _timeProvider;
        private readonly FarmDatabaseSO _database;
        private readonly IStorageService _storageService;
        private readonly IPublisher<FarmSlotChangedPayload> _slotChangedPub;
        private readonly IDisposable _tickSubscription;

        private readonly Dictionary<Vector3Int, FarmSlotSaveData> _slots = new Dictionary<Vector3Int, FarmSlotSaveData>();
        private readonly List<FarmSlotSaveData> _activeSlotsList = new List<FarmSlotSaveData>();
        private List<FarmSlotSaveData> _persistedSlotsList;

        public IReadOnlyList<FarmSlotSaveData> ActiveSlots => _activeSlotsList;

        public FarmService(
            IServerTimeProvider timeProvider,
            FarmDatabaseSO database,
            IStorageService storageService,
            ISubscriber<ClockTickPayload> tickSub,
            IPublisher<FarmSlotChangedPayload> slotChangedPub)
        {
            _timeProvider = timeProvider;
            _database = database;
            _storageService = storageService;
            _slotChangedPub = slotChangedPub;

            // Subscribe to ClockService tick event (publishes every 1s)
            _tickSubscription = tickSub.Subscribe(OnClockTick);
        }

        public void Initialize(List<FarmSlotSaveData> savedSlots, long lastSaveUtcTicks)
        {
            _persistedSlotsList = savedSlots;
            _slots.Clear();
            _activeSlotsList.Clear();
            _database.InitializeLookups();

            foreach (var slot in savedSlots)
            {
                var cell = new Vector3Int(slot.cellX, slot.cellY, slot.cellZ);
                _slots[cell] = slot;
                _activeSlotsList.Add(slot);
            }

            CalculateOfflineProgress(lastSaveUtcTicks);
        }

        private void OnClockTick(ClockTickPayload payload)
        {
            if (_storageService.IsCheatDetected) return;

            bool anyChanged = false;
            long nowTicks = _timeProvider.UtcNow.Ticks;

            foreach (var slot in _activeSlotsList)
            {
                if (slot.state == FarmSlotState.Growing)
                {
                    float requiredTime = GetRequiredTime(slot);

                    slot.growthTimeSec += 1f;
                    slot.lastUpdateUtcTicks = nowTicks;
                    anyChanged = true;

                    if (slot.growthTimeSec >= requiredTime)
                    {
                        slot.state = FarmSlotState.Ripe;
                        slot.growthTimeSec = requiredTime;
                    }

                    _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
                }
            }

            if (anyChanged) _storageService.Save();
        }

        private void CalculateOfflineProgress(long lastSaveTicks)
        {
            if (_storageService.IsCheatDetected) return;
            if (lastSaveTicks <= 0) return;

            long nowTicks = _timeProvider.UtcNow.Ticks;
            double elapsedSeconds = TimeSpan.FromTicks(nowTicks - lastSaveTicks).TotalSeconds;

            if (elapsedSeconds <= 0) return;

            bool anyChanged = false;
            foreach (var slot in _activeSlotsList)
            {
                if (slot.state == FarmSlotState.Growing)
                {
                    float requiredTime = GetRequiredTime(slot);
                    slot.growthTimeSec += (float)elapsedSeconds;
                    slot.lastUpdateUtcTicks = nowTicks;
                    anyChanged = true;

                    if (slot.growthTimeSec >= requiredTime)
                    {
                        slot.state = FarmSlotState.Ripe;
                        slot.growthTimeSec = requiredTime;
                    }
                    _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
                }
            }

            if (anyChanged) _storageService.Save();
        }

        public bool TryPlant(Vector3Int cell, string entityId, bool isAnimal)
        {
            if (_storageService.IsCheatDetected) return false;

            if (_slots.TryGetValue(cell, out var existingSlot))
            {
                if (existingSlot.state != FarmSlotState.Empty) return false;
            }

            int cost = 0;
            if (isAnimal)
            {
                var data = _database.GetAnimalById(entityId);
                if (data == null) return false;
                cost = data.purchaseCost;
            }
            else
            {
                var data = _database.GetCropById(entityId);
                if (data == null) return false;
                cost = data.coinCost;
            }

            if (_storageService.Coins < cost)
            {
                Debug.LogWarning($"[FarmService] Not enough coins to plant {entityId}. Cost: {cost}, Has: {_storageService.Coins}");
                return false;
            }

            _storageService.Coins -= cost;

            var slot = existingSlot ?? new FarmSlotSaveData { cellX = cell.x, cellY = cell.y, cellZ = cell.z };
            slot.isAnimal = isAnimal;
            slot.entityId = entityId;
            // Animals start as Empty and need to be Fed to grow/produce. Crops start as Growing immediately.
            slot.state = isAnimal ? FarmSlotState.Empty : FarmSlotState.Growing;
            slot.growthTimeSec = 0;
            slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
            slot.lastUpdateUtcTicks = slot.startTimeUtcTicks;
            slot.isFed = false;

            if (!isAnimal)
            {
                var data = _database.GetCropById(entityId);
                slot.remainingHarvests = data.maxHarvestBatches;
            }

            if (!_slots.ContainsKey(cell))
            {
                _slots[cell] = slot;
                _activeSlotsList.Add(slot);
                _persistedSlotsList.Add(slot);
            }

            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
            _storageService.Save();
            return true;
        }

        public bool TryFeed(Vector3Int cell)
        {
            if (_storageService.IsCheatDetected) return false;
            if (!_slots.TryGetValue(cell, out var slot) || !slot.isAnimal || slot.state != FarmSlotState.Empty) return false;

            var data = _database.GetAnimalById(slot.entityId);
            if (data == null) return false;

            if (_storageService.RemoveItem(data.requiredFoodItemId, 1))
            {
                slot.state = FarmSlotState.Growing;
                slot.isFed = true;
                slot.growthTimeSec = 0;
                slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
                slot.lastUpdateUtcTicks = slot.startTimeUtcTicks;

                _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
                _storageService.Save();
                return true;
            }
            else
            {
                Debug.LogWarning($"[FarmService] Missing required food item: {data.requiredFoodItemId}");
            }
            return false;
        }

        public bool TryHarvest(Vector3Int cell, out string productItemId, out int amount)
        {
            productItemId = null;
            amount = 0;

            if (_storageService.IsCheatDetected) return false;
            if (!_slots.TryGetValue(cell, out var slot) || slot.state != FarmSlotState.Ripe) return false;

            if (slot.isAnimal)
            {
                var data = _database.GetAnimalById(slot.entityId);
                if (data == null) return false;
                productItemId = data.yieldItemId;
                amount = data.productAmount;

                // Reset back to Empty (unfed) state for the next feeding cycle
                slot.state = FarmSlotState.Empty;
                slot.isFed = false;
                slot.growthTimeSec = 0;
            }
            else
            {
                var data = _database.GetCropById(slot.entityId);
                if (data == null) return false;
                productItemId = data.yieldItemId;
                amount = data.harvestAmount;

                slot.remainingHarvests--;
                if (slot.remainingHarvests > 0)
                {
                    // Multi-harvest (Sugarcane): reset to Growing state
                    slot.state = FarmSlotState.Growing;
                    slot.growthTimeSec = 0;
                    slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
                }
                else
                {
                    // Empty the slot when no harvests remain
                    slot.state = FarmSlotState.Empty;
                    slot.entityId = null;
                    slot.growthTimeSec = 0;
                }
            }

            _storageService.AddItem(productItemId, amount);
            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
            _storageService.Save();
            return true;
        }

        private float GetRequiredTime(FarmSlotSaveData slot)
        {
            if (slot.isAnimal)
                return _database.GetAnimalById(slot.entityId).productionTime;
            return _database.GetCropById(slot.entityId).growTime;
        }

        public FarmSlotSaveData GetSlotAt(Vector3Int cell)
        {
            return _slots.TryGetValue(cell, out var slot) ? slot : null;
        }

        public void Dispose()
        {
            _tickSubscription?.Dispose();
        }
    }
}
