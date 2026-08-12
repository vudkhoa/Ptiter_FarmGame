using System;
using System.Collections.Generic;
using Core.Module.Time;
using Core.Module.Storage;
using Core.Module.Map;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Farm
{
    public class FarmService : IFarmService, IMapPlacementRemovalPolicy, IDisposable
    {
        /// <summary>Sentinel so a slot whose entity is missing from the database never ripens.</summary>
        private const float MissingEntityFallbackTime = 9999f;

        /// <summary>remainingHarvests value meaning "never runs out" (livestock).</summary>
        private const int InfiniteHarvests = -1;

        private readonly IServerTimeProvider _timeProvider;
        private readonly FarmDatabaseSO _database;
        private readonly IStorageService _storageService;
        private readonly IPublisher<FarmSlotChangedPayload> _slotChangedPub;
        private readonly IPublisher<FarmEntityPlantedPayload> _plantedPub;
        private readonly IPublisher<FarmEntityCaredPayload> _caredPub;
        private readonly IPublisher<FarmEntityStageChangedPayload> _stageChangedPub;
        private readonly IPublisher<FarmEntityRipePayload> _ripePub;
        private readonly IPublisher<FarmEntityHarvestedPayload> _harvestedPub;
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
            IPublisher<FarmSlotChangedPayload> slotChangedPub,
            IPublisher<FarmEntityPlantedPayload> plantedPub,
            IPublisher<FarmEntityCaredPayload> caredPub,
            IPublisher<FarmEntityStageChangedPayload> stageChangedPub,
            IPublisher<FarmEntityRipePayload> ripePub,
            IPublisher<FarmEntityHarvestedPayload> harvestedPub,
            IFarmSaveSource saveSource)
        {
            _timeProvider = timeProvider;
            _database = database;
            _storageService = storageService;
            _slotChangedPub = slotChangedPub;
            _plantedPub = plantedPub;
            _caredPub = caredPub;
            _stageChangedPub = stageChangedPub;
            _ripePub = ripePub;
            _harvestedPub = harvestedPub;

            // ClockService publishes one tick per second.
            _tickSubscription = tickSub.Subscribe(OnClockTick);

            // Load save here so the service never exists in a "constructed but not initialized" state.
            if (saveSource?.FarmSlots == null)
            {
                Debug.LogError("[FarmService] No save slots available - player data is not loaded yet. Farm will be empty and planting will not persist.");
                return;
            }

            Initialize(saveSource.FarmSlots, saveSource.LastSaveUtcTicks);
        }

        public void Initialize(List<FarmSlotSaveData> savedSlots, long lastSaveUtcTicks)
        {
            _persistedSlotsList = savedSlots;
            _slots.Clear();
            _activeSlotsList.Clear();
            _database.InitializeLookups();

            for (int i = 0; i < savedSlots.Count; i++)
            {
                var slot = savedSlots[i];
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
            long nowTicks = payload.UtcNow.Ticks;

            for (int i = 0; i < _activeSlotsList.Count; i++)
            {
                var slot = _activeSlotsList[i];
                if (slot.state != FarmSlotState.Growing) continue;

                float elapsed = (float)TimeSpan.FromTicks(nowTicks - slot.lastUpdateUtcTicks).TotalSeconds;

                // Skip negative deltas caused by small clock jitter.
                if (elapsed <= 0f) continue;

                ProgressGrowth(slot, elapsed, nowTicks);
                anyChanged = true;
            }

            if (anyChanged) _storageService.Save();
        }

        /// <summary>Catches growth up to real elapsed time after the game was closed.</summary>
        private void CalculateOfflineProgress(long lastSaveTicks)
        {
            if (_storageService.IsCheatDetected) return;
            if (lastSaveTicks <= 0) return;

            long nowTicks = _timeProvider.UtcNow.Ticks;
            double elapsedSeconds = TimeSpan.FromTicks(nowTicks - lastSaveTicks).TotalSeconds;

            if (elapsedSeconds <= 0) return;

            bool anyChanged = false;
            for (int i = 0; i < _activeSlotsList.Count; i++)
            {
                var slot = _activeSlotsList[i];
                if (slot.state != FarmSlotState.Growing) continue;

                ProgressGrowth(slot, (float)elapsedSeconds, nowTicks);
                anyChanged = true;
            }

            if (anyChanged) _storageService.Save();
        }

        /// <summary>Advances one slot's growth timer and flips it to Ripe when it is done.</summary>
        private void ProgressGrowth(FarmSlotSaveData slot, float elapsedSeconds, long nowTicks)
        {
            float requiredTime = GetRequiredTime(slot);
            var entity = _database.GetEntityById(slot.entityId);
            float stage2Threshold = entity != null ? entity.stage2Threshold : 0.3f;
            FarmEntityType entityType = entity != null ? entity.entityType : FarmEntityType.Crop;

            float oldProgress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;

            slot.growthTimeSec += elapsedSeconds;
            slot.lastUpdateUtcTicks = nowTicks;

            float newProgress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;

            if (slot.growthTimeSec >= requiredTime)
            {
                slot.state = FarmSlotState.Ripe;
                slot.growthTimeSec = requiredTime;
                _ripePub.Publish(new FarmEntityRipePayload(slot.entityId, new Vector3Int(slot.cellX, slot.cellY, slot.cellZ), entityType));
            }
            else
            {
                int totalSprites = (entity != null && entity.growthSprites != null) ? entity.growthSprites.Length : 0;
                if (totalSprites >= 3)
                {
                    if (oldProgress < stage2Threshold && newProgress >= stage2Threshold)
                    {
                        _stageChangedPub.Publish(new FarmEntityStageChangedPayload(slot.entityId, new Vector3Int(slot.cellX, slot.cellY, slot.cellZ), entityType, 1));
                    }
                }
            }

            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
        }

        public bool TryPlant(Vector3Int cell, string entityId)
        {
            if (_storageService.IsCheatDetected) return false;

            if (_slots.TryGetValue(cell, out var existingSlot))
            {
                if (existingSlot.state != FarmSlotState.Empty) return false;
            }

            var entity = _database.GetEntityById(entityId);
            if (entity == null) return false;

            if (_storageService.Coins < entity.coinCost)
            {
                Debug.LogWarning($"[FarmService] Cannot plant {entityId}: costs {entity.coinCost} coins but player has {_storageService.Coins}.");
                return false;
            }

            _storageService.Coins -= entity.coinCost;

            var slot = existingSlot ?? new FarmSlotSaveData { cellX = cell.x, cellY = cell.y, cellZ = cell.z };
            slot.entityId = entityId;
            slot.growthTimeSec = 0;
            slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
            slot.lastUpdateUtcTicks = slot.startTimeUtcTicks;

            // Crops start growing immediately; animals wait in Empty until they are fed.
            bool hasInputs = entity.inputs != null && entity.inputs.Length > 0;
            slot.state = hasInputs ? FarmSlotState.Empty : FarmSlotState.Growing;
            slot.isFed = !hasInputs;
            slot.remainingHarvests = entity.maxCycles;

            if (!_slots.ContainsKey(cell))
            {
                _slots[cell] = slot;
                _activeSlotsList.Add(slot);
                if (_persistedSlotsList != null)
                {
                    _persistedSlotsList.Add(slot);
                }
            }

            _plantedPub.Publish(new FarmEntityPlantedPayload(entityId, cell, entity.entityType));
            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
            _storageService.Save();
            return true;
        }

        public bool TryFeed(Vector3Int cell)
        {
            if (_storageService.IsCheatDetected) return false;
            if (!_slots.TryGetValue(cell, out var slot) || slot.state != FarmSlotState.Empty) return false;

            var entity = _database.GetEntityById(slot.entityId);
            if (entity == null || entity.inputs == null) return false;

            if (!HasAllInputs(entity))
            {
                Debug.LogWarning($"[FarmService] Cannot feed {entity.entityName}: required items are missing from inventory.");
                return false;
            }

            for (int i = 0; i < entity.inputs.Length; i++)
            {
                var req = entity.inputs[i];
                _storageService.RemoveItem(req.item.ItemId, req.amount);
            }

            slot.state = FarmSlotState.Growing;
            slot.isFed = true;
            slot.growthTimeSec = 0;
            slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
            slot.lastUpdateUtcTicks = slot.startTimeUtcTicks;

            _caredPub.Publish(new FarmEntityCaredPayload(slot.entityId, cell, entity.entityType, entity.inputs));
            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
            _storageService.Save();
            return true;
        }

        private bool HasAllInputs(FarmEntityData entity)
        {
            for (int i = 0; i < entity.inputs.Length; i++)
            {
                var req = entity.inputs[i];
                if (req.item == null || _storageService.GetItemCount(req.item.ItemId) < req.amount) return false;
            }

            return true;
        }

        public bool TryHarvest(Vector3Int cell, out string productItemId, out int amount)
        {
            productItemId = null;
            amount = 0;

            if (_storageService.IsCheatDetected) return false;
            if (!_slots.TryGetValue(cell, out var slot) || slot.state != FarmSlotState.Ripe) return false;

            var entity = _database.GetEntityById(slot.entityId);
            if (entity == null || entity.outputs == null || entity.outputs.Length == 0) return false;

            for (int i = 0; i < entity.outputs.Length; i++)
            {
                var reward = entity.outputs[i];
                if (reward.item == null) continue;

                // The first reward is the one the UI shows floating up.
                if (productItemId == null)
                {
                    productItemId = reward.item.ItemId;
                    amount = reward.amount;
                }
            }

            ApplyPostHarvestState(slot, entity);

            _harvestedPub.Publish(new FarmEntityHarvestedPayload(
                entity.EntityId,
                cell,
                entity.entityType,
                entity.outputs));
            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
            _storageService.Save();
            return true;
        }

        public bool TryApplyItem(Vector3Int cell, ItemDataSO item)
        {
            if (_storageService.IsCheatDetected) return false;
            if (item == null) return false;
            if (!_slots.TryGetValue(cell, out var slot)) return false;

            var entity = _database.GetEntityById(slot.entityId);
            if (entity == null) return false;

            if (slot.state == FarmSlotState.Growing && item is BoosterItemDataSO boosterItem)
            {
                if (_storageService.GetItemCount(item.ItemId) < 1)
                {
                    Debug.LogWarning($"[FarmService] Player does not have booster item {item.ItemId} in inventory.");
                    return false;
                }

                _storageService.RemoveItem(item.ItemId, 1);

                float requiredTime = GetRequiredTime(slot);
                float stage2Threshold = entity.stage2Threshold;
                float oldProgress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;

                slot.growthTimeSec += boosterItem.boostAmountSec;
                if (slot.growthTimeSec >= requiredTime)
                {
                    slot.growthTimeSec = requiredTime;
                    slot.state = FarmSlotState.Ripe;
                    _ripePub.Publish(new FarmEntityRipePayload(slot.entityId, cell, entity.entityType));
                }
                else
                {
                    float newProgress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;
                    int totalSprites = entity.growthSprites != null ? entity.growthSprites.Length : 0;
                    if (totalSprites >= 3)
                    {
                        if (oldProgress < stage2Threshold && newProgress >= stage2Threshold)
                        {
                            _stageChangedPub.Publish(new FarmEntityStageChangedPayload(slot.entityId, cell, entity.entityType, 1));
                        }
                    }
                }

                _caredPub.Publish(new FarmEntityCaredPayload(
                    slot.entityId,
                    cell,
                    entity.entityType,
                    new InputRequirement[] { new InputRequirement { item = item, amount = 1 } }
                ));

                _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));
                _storageService.Save();
                return true;
            }

            return false;
        }

        /// <summary>Decides what a slot becomes after being harvested: reset, regrow, or clear.</summary>
        private void ApplyPostHarvestState(FarmSlotSaveData slot, FarmEntityData entity)
        {
            if (entity.entityType == FarmEntityType.Animal)
            {
                slot.isAdult = true;
            }

            // Livestock produces forever: back to Empty, waiting to be fed again.
            if (slot.remainingHarvests == InfiniteHarvests)
            {
                slot.state = FarmSlotState.Empty;
                slot.isFed = false;
                slot.growthTimeSec = 0;
                return;
            }

            slot.remainingHarvests--;

            // Multi-harvest crops such as sugarcane regrow until they run out of cycles.
            if (slot.remainingHarvests > 0)
            {
                slot.state = entity.autoRestart ? FarmSlotState.Growing : FarmSlotState.Empty;
                slot.isFed = !entity.autoRestart;
                slot.growthTimeSec = 0;
                slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
                return;
            }

            // Last cycle used up: the tile becomes free land again.
            slot.state = FarmSlotState.Empty;
            slot.entityId = null;
            slot.growthTimeSec = 0;
            slot.isFed = false;
        }

        private float GetRequiredTime(FarmSlotSaveData slot)
        {
            var entity = _database.GetEntityById(slot.entityId);
            return entity != null ? entity.processTime : MissingEntityFallbackTime;
        }

        public FarmSlotSaveData GetSlotAt(Vector3Int cell)
        {
            return _slots.TryGetValue(cell, out var slot) ? slot : null;
        }

        public bool CanRemove(in MapPlacementRemovalContext context)
        {
            if (context.FarmRole == FarmObjectRole.Barn)
                return true;

            if (context.FarmRole != FarmObjectRole.Soil)
                return true;

            FarmSlotSaveData slot = GetSlotAt(context.OriginCell);
            return slot == null || string.IsNullOrEmpty(slot.entityId);
        }

        public void OnRemoved(in MapPlacementRemovalContext context)
        {
            if (context.FarmRole != FarmObjectRole.Soil && context.FarmRole != FarmObjectRole.Barn)
                return;

            if (!_slots.TryGetValue(context.OriginCell, out FarmSlotSaveData slot)) return;
            if (context.FarmRole == FarmObjectRole.Soil && !string.IsNullOrEmpty(slot.entityId)) return;

            // Removing a barn currently removes its animal as well. Publish an
            // empty slot first so FarmVisualizer destroys the separate animal view.
            slot.entityId = null;
            slot.state = FarmSlotState.Empty;
            slot.growthTimeSec = 0f;
            slot.startTimeUtcTicks = 0L;
            slot.lastUpdateUtcTicks = 0L;
            slot.remainingHarvests = 0;
            slot.isFed = false;
            slot.isAdult = false;
            _slotChangedPub.Publish(new FarmSlotChangedPayload(slot));

            _slots.Remove(context.OriginCell);
            _activeSlotsList.Remove(slot);
            _persistedSlotsList?.Remove(slot);
        }

        public void Dispose()
        {
            _tickSubscription?.Dispose();
        }
    }
}
