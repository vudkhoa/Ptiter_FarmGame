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
            IPublisher<FarmEntityHarvestedPayload> harvestedPub)
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
            long nowTicks = payload.UtcNow.Ticks;

            foreach (var slot in _activeSlotsList)
            {
                if (slot.state == FarmSlotState.Growing)
                {
                    float elapsed = (float)TimeSpan.FromTicks(nowTicks - slot.lastUpdateUtcTicks).TotalSeconds;
                    // Đảm bảo không cộng thời gian âm nếu có sai số nhỏ ở clock
                    if (elapsed > 0f)
                    {
                        ProgressGrowth(slot, elapsed, nowTicks);
                        anyChanged = true;
                    }
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
                    ProgressGrowth(slot, (float)elapsedSeconds, nowTicks);
                    anyChanged = true;
                }
            }

            if (anyChanged) _storageService.Save();
        }

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
                Debug.LogWarning($"[FarmService] Not enough coins to purchase {entityId}. Cost: {entity.coinCost}, Has: {_storageService.Coins}");
                return false;
            }

            _storageService.Coins -= entity.coinCost;

            var slot = existingSlot ?? new FarmSlotSaveData { cellX = cell.x, cellY = cell.y, cellZ = cell.z };
            slot.entityId = entityId;
            slot.growthTimeSec = 0;
            slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
            slot.lastUpdateUtcTicks = slot.startTimeUtcTicks;

            // Xử lý Generic: Cây trồng tự động Grow, Động vật bắt đầu dạng Empty (chờ cho ăn)
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

            // Kiểm tra đủ toàn bộ inputs yêu cầu hay không
            bool hasAllInputs = true;
            foreach (var req in entity.inputs)
            {
                if (req.item == null || _storageService.GetItemCount(req.item.ItemId) < req.amount)
                {
                    hasAllInputs = false;
                    break;
                }
            }

            if (hasAllInputs)
            {
                // Tiêu hao các nguyên liệu đầu vào
                foreach (var req in entity.inputs)
                {
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
            else
            {
                Debug.LogWarning($"[FarmService] Missing required food/materials for {entity.entityName}");
            }
            return false;
        }

        public bool TryHarvest(Vector3Int cell, out string productItemId, out int amount)
        {
            productItemId = null;
            amount = 0;

            if (_storageService.IsCheatDetected) return false;
            if (!_slots.TryGetValue(cell, out var slot) || slot.state != FarmSlotState.Ripe) return false;

            var entity = _database.GetEntityById(slot.entityId);
            if (entity == null || entity.outputs == null || entity.outputs.Length == 0) return false;

            // 1. Thêm toàn bộ các sản phẩm outputs vào kho đồ
            for (int i = 0; i < entity.outputs.Length; i++)
            {
                var reward = entity.outputs[i];
                if (reward.item == null) continue;
                
                _storageService.AddItem(reward.item.ItemId, reward.amount);

                if (i == 0)
                {
                    // Lấy sản phẩm đầu tiên làm đại diện bay lên UI
                    productItemId = reward.item.ItemId;
                    amount = reward.amount;
                }
            }

            // 2. Cập nhật trạng thái vòng đời sau thu hoạch
            bool isAnimal = entity.entityType == FarmEntityType.Animal;
            if (isAnimal)
            {
                slot.isAdult = true; // Đánh dấu trưởng thành
            }

            if (slot.remainingHarvests == -1) // Vật nuôi chạy vô hạn chu kỳ
            {
                slot.state = FarmSlotState.Empty;
                slot.isFed = false;
                slot.growthTimeSec = 0;
            }
            else // Cây trồng có giới hạn vòng đời
            {
                slot.remainingHarvests--;
                if (slot.remainingHarvests > 0)
                {
                    // Thu hoạch nhiều đợt (ví dụ: Mía): quay lại lớn tiếp nếu mọc lại tự động
                    slot.state = entity.autoRestart ? FarmSlotState.Growing : FarmSlotState.Empty;
                    slot.isFed = !entity.autoRestart;
                    slot.growthTimeSec = 0;
                    slot.startTimeUtcTicks = _timeProvider.UtcNow.Ticks;
                }
                else
                {
                    // Ô đất trống trở lại khi hết đợt thu hoạch
                    slot.state = FarmSlotState.Empty;
                    slot.entityId = null;
                    slot.growthTimeSec = 0;
                    slot.isFed = false;
                }
            }

            _harvestedPub.Publish(new FarmEntityHarvestedPayload(entity.EntityId, cell, productItemId, amount, entity.entityType));
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

        private float GetRequiredTime(FarmSlotSaveData slot)
        {
            var entity = _database.GetEntityById(slot.entityId);
            return entity != null ? entity.processTime : 9999f;
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
