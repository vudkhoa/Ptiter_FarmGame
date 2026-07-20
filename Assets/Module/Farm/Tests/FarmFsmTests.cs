using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Core.Module.Time;
using Core.Module.Farm;
using Core.Module.Storage;
using MessagePipe;

namespace Core.Module.Farm.Tests
{
    [TestFixture]
    public class FarmFsmTests
    {
        private MockTimeProvider _mockTimeProvider;
        private MockInventoryProvider _mockInventory;
        private FarmDatabaseSO _mockDatabase;
        
        private FarmEntityData _sampleCrop;
        private FarmEntityData _sampleAnimal;

        private ItemDataSO _wheatGrainItem;
        private ItemDataSO _eggItem;

        // Recording Publishers for assertions
        private RecordingPublisher<FarmSlotChangedPayload> _slotChangedPub;
        private RecordingPublisher<FarmEntityPlantedPayload> _plantedPub;
        private RecordingPublisher<FarmEntityCaredPayload> _caredPub;
        private RecordingPublisher<FarmEntityStageChangedPayload> _stageChangedPub;
        private RecordingPublisher<FarmEntityRipePayload> _ripePub;
        private RecordingPublisher<FarmEntityHarvestedPayload> _harvestedPub;

        private class StubDisposable : IDisposable
        {
            public void Dispose() {}
        }

        private class StubSubscriber<T> : ISubscriber<T>
        {
            public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
            {
                return new StubDisposable();
            }
        }

        private class RecordingPublisher<T> : IPublisher<T>
        {
            public readonly List<T> Published = new List<T>();
            public void Publish(T message)
            {
                Published.Add(message);
            }
        }

        private class MockTimeProvider : IServerTimeProvider
        {
            public DateTime CurrentTime { get; set; } = DateTime.UtcNow;
            public DateTime UtcNow => CurrentTime;
            public bool IsSynced => true;
            public TimeSpan Offset => TimeSpan.Zero;
            public DateTime LastSyncedAt => CurrentTime;
        }

        private class MockInventoryProvider : IStorageService
        {
            public int Coins { get; set; } = 1000;
            public bool IsCheatDetected { get; set; } = false;
            public long LastSaveUtcTicks { get; set; } = 0;
            public Dictionary<string, int> Inv = new Dictionary<string, int>();
            
            public List<InventoryChangedPayload> PublishedEvents = new List<InventoryChangedPayload>();

            public int GetItemCount(string itemId) => Inv.TryGetValue(itemId, out var a) ? a : 0;
            
            public void AddItem(string itemId, int amount)
            {
                Inv[itemId] = GetItemCount(itemId) + amount;
                PublishedEvents.Add(new InventoryChangedPayload(itemId, Inv[itemId], amount));
            }
            
            public bool RemoveItem(string itemId, int amount)
            {
                int count = GetItemCount(itemId);
                if (count < amount) return false;
                Inv[itemId] = count - amount;
                PublishedEvents.Add(new InventoryChangedPayload(itemId, Inv[itemId], -amount));
                return true;
            }
            
            public void Save() { LastSaveUtcTicks = CurrentTicks; }
            public long CurrentTicks { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            _mockTimeProvider = new MockTimeProvider();
            _mockInventory = new MockInventoryProvider();

            // Setup mock ItemDataSO
            _wheatGrainItem = ScriptableObject.CreateInstance<ItemDataSO>();
            _wheatGrainItem.name = "wheat_grain";
            _wheatGrainItem.displayName = "Wheat Grain";

            _eggItem = ScriptableObject.CreateInstance<ItemDataSO>();
            _eggItem.name = "egg";
            _eggItem.displayName = "Egg";

            // Setup Crop mockup: wheat grows in 10s
            _sampleCrop = ScriptableObject.CreateInstance<FarmEntityData>();
            _sampleCrop.name = "wheat";
            _sampleCrop.entityName = "Wheat";
            _sampleCrop.entityType = FarmEntityType.Crop;
            _sampleCrop.processTime = 10f;
            _sampleCrop.coinCost = 10;
            _sampleCrop.outputs = new OutputReward[] { new OutputReward { item = _wheatGrainItem, amount = 2 } };
            _sampleCrop.autoRestart = true;
            _sampleCrop.maxCycles = 1;
            _sampleCrop.stage2Threshold = 0.3f;
            _sampleCrop.growthSprites = new Sprite[3];

            // Setup Animal mockup: chicken produces in 15s
            _sampleAnimal = ScriptableObject.CreateInstance<FarmEntityData>();
            _sampleAnimal.name = "chicken";
            _sampleAnimal.entityName = "Chicken";
            _sampleAnimal.entityType = FarmEntityType.Animal;
            _sampleAnimal.processTime = 15f;
            _sampleAnimal.coinCost = 50;
            _sampleAnimal.inputs = new InputRequirement[] { new InputRequirement { item = _wheatGrainItem, amount = 1 } };
            _sampleAnimal.outputs = new OutputReward[] { new OutputReward { item = _eggItem, amount = 1 } };
            _sampleAnimal.autoRestart = false;
            _sampleAnimal.maxCycles = -1;
            _sampleAnimal.growthSprites = new Sprite[3];

            // Setup Database mockup
            _mockDatabase = ScriptableObject.CreateInstance<FarmDatabaseSO>();
            var allEntitiesField = typeof(FarmDatabaseSO).GetField("allEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            allEntitiesField.SetValue(_mockDatabase, new List<FarmEntityData> { _sampleCrop, _sampleAnimal });
            _mockDatabase.InitializeLookups();
        }

        private FarmService CreateFarmService(List<FarmSlotSaveData> savedSlots = null)
        {
            _slotChangedPub = new RecordingPublisher<FarmSlotChangedPayload>();
            _plantedPub = new RecordingPublisher<FarmEntityPlantedPayload>();
            _caredPub = new RecordingPublisher<FarmEntityCaredPayload>();
            _stageChangedPub = new RecordingPublisher<FarmEntityStageChangedPayload>();
            _ripePub = new RecordingPublisher<FarmEntityRipePayload>();
            _harvestedPub = new RecordingPublisher<FarmEntityHarvestedPayload>();

            var service = new FarmService(
                _mockTimeProvider,
                _mockDatabase,
                _mockInventory,
                new StubSubscriber<ClockTickPayload>(),
                _slotChangedPub,
                _plantedPub,
                _caredPub,
                _stageChangedPub,
                _ripePub,
                _harvestedPub
            );
            service.Initialize(savedSlots ?? new List<FarmSlotSaveData>(), 0);
            return service;
        }

        private void SimulateClockTicks(FarmService service, int totalSeconds)
        {
            var clockTickMethod = typeof(FarmService).GetMethod("OnClockTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            for (int i = 0; i < totalSeconds; i++)
            {
                _mockTimeProvider.CurrentTime = _mockTimeProvider.CurrentTime.AddSeconds(1);
                clockTickMethod.Invoke(service, new object[] { new ClockTickPayload(i + 1, _mockTimeProvider.UtcNow) });
            }
        }

        #region Nhóm Test Gieo Trồng (Planting Tests)

        [Test]
        public void Test_Plant_Success_Publishes_PlantedEvent()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            bool success = service.TryPlant(cell, "wheat");
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.IsNotNull(slot);
            Assert.AreEqual("wheat", slot.entityId);
            Assert.AreEqual(FarmSlotState.Growing, slot.state);
            Assert.AreEqual(990, _mockInventory.Coins);

            // Assert planted event was fired
            Assert.AreEqual(1, _plantedPub.Published.Count);
            var plantedEvent = _plantedPub.Published[0];
            Assert.AreEqual("wheat", plantedEvent.EntityId);
            Assert.AreEqual(cell, plantedEvent.Cell);
            Assert.AreEqual(FarmEntityType.Crop, plantedEvent.EntityType);
        }

        [Test]
        public void Test_Plant_Fails_If_NotEnoughCoins()
        {
            _mockInventory.Coins = 5;
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            bool success = service.TryPlant(cell, "wheat");
            var slot = service.GetSlotAt(cell);

            Assert.IsFalse(success);
            Assert.IsNull(slot);
            Assert.AreEqual(0, _plantedPub.Published.Count);
        }

        [Test]
        public void Test_Plant_Fails_If_SlotNotEmpty()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            service.TryPlant(cell, "wheat");
            bool success = service.TryPlant(cell, "chicken");

            Assert.IsFalse(success);
            Assert.AreEqual(1, _plantedPub.Published.Count); // only first plant published
        }

        #endregion

        #region Nhóm Test Chăm Sóc / Cho Ăn (Caring / Feeding Tests)

        [Test]
        public void Test_Feed_Animal_Success_ConsumesItems_And_Publishes_CaredEvent()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            // Buy chicken (starts Empty and unfed because it needs wheat_grain)
            service.TryPlant(cell, "chicken");
            var slot = service.GetSlotAt(cell);
            Assert.AreEqual(FarmSlotState.Empty, slot.state);
            Assert.IsFalse(slot.isFed);

            // Provide wheat_grain and care/feed
            _mockInventory.AddItem("wheat_grain", 1);
            bool success = service.TryFeed(cell);

            Assert.IsTrue(success);
            Assert.IsTrue(slot.isFed);
            Assert.AreEqual(FarmSlotState.Growing, slot.state);
            Assert.AreEqual(0, _mockInventory.GetItemCount("wheat_grain"));

            // Assert cared event was fired
            Assert.AreEqual(1, _caredPub.Published.Count);
            var caredEvent = _caredPub.Published[0];
            Assert.AreEqual("chicken", caredEvent.EntityId);
            Assert.AreEqual(cell, caredEvent.Cell);
            Assert.AreEqual(FarmEntityType.Animal, caredEvent.EntityType);
            Assert.AreEqual(1, caredEvent.InputsApplied.Length);
            Assert.AreEqual("wheat_grain", caredEvent.InputsApplied[0].item.ItemId);
        }

        [Test]
        public void Test_Feed_Crop_Success_ConsumesItems_And_Publishes_CaredEvent()
        {
            // Give wheat crop an input requirement for fertilizer/watering simulation
            _sampleCrop.inputs = new InputRequirement[] { new InputRequirement { item = _wheatGrainItem, amount = 1 } };

            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            // Plant wheat (will start Empty because it now has inputs)
            service.TryPlant(cell, "wheat");
            var slot = service.GetSlotAt(cell);
            Assert.AreEqual(FarmSlotState.Empty, slot.state);

            // Feed/Water/Fertilize crop
            _mockInventory.AddItem("wheat_grain", 1);
            bool success = service.TryFeed(cell);

            Assert.IsTrue(success);
            Assert.AreEqual(FarmSlotState.Growing, slot.state);

            // Assert cared event was fired
            Assert.AreEqual(1, _caredPub.Published.Count);
            var caredEvent = _caredPub.Published[0];
            Assert.AreEqual("wheat", caredEvent.EntityId);
            Assert.AreEqual(FarmEntityType.Crop, caredEvent.EntityType);
        }

        [Test]
        public void Test_Feed_Fails_If_MissingInputs()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);

            service.TryPlant(cell, "chicken");
            bool success = service.TryFeed(cell);

            Assert.IsFalse(success);
            var slot = service.GetSlotAt(cell);
            Assert.AreEqual(FarmSlotState.Empty, slot.state);
            Assert.IsFalse(slot.isFed);
            Assert.AreEqual(0, _caredPub.Published.Count);
        }

        #endregion

        #region Nhóm Test Tăng Trưởng (Growing / Ripening Tests)

        [Test]
        public void Test_Growth_Tick_Publishes_StageChangedEvent_OnThreshold()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Tick 2 seconds (progress = 20% < 30%) -> no stage change
            SimulateClockTicks(service, 2);
            Assert.AreEqual(0, _stageChangedPub.Published.Count);

            // Tick 2 more seconds (total 4s, progress = 40% >= 30%) -> stage change event
            SimulateClockTicks(service, 2);
            Assert.AreEqual(1, _stageChangedPub.Published.Count);
            var stageEvent = _stageChangedPub.Published[0];
            Assert.AreEqual("wheat", stageEvent.EntityId);
            Assert.AreEqual(cell, stageEvent.Cell);
            Assert.AreEqual(1, stageEvent.NewStage);
            Assert.AreEqual(FarmEntityType.Crop, stageEvent.EntityType);
        }

        [Test]
        public void Test_Growth_Tick_NoStageChangedEvent_IfEntityHasOnlyTwoStages()
        {
            _sampleCrop.growthSprites = new Sprite[2];

            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            SimulateClockTicks(service, 4);

            Assert.AreEqual(0, _stageChangedPub.Published.Count);
        }

        [Test]
        public void Test_Growth_Tick_Publishes_RipeEvent_OnCompletion()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Tick 10 seconds to fully ripen
            SimulateClockTicks(service, 10);
            var slot = service.GetSlotAt(cell);

            Assert.AreEqual(FarmSlotState.Ripe, slot.state);
            Assert.AreEqual(1, _ripePub.Published.Count);
            var ripeEvent = _ripePub.Published[0];
            Assert.AreEqual("wheat", ripeEvent.EntityId);
            Assert.AreEqual(cell, ripeEvent.Cell);
            Assert.AreEqual(FarmEntityType.Crop, ripeEvent.EntityType);
        }

        [Test]
        public void Test_Growth_Freeze_If_CheatDetected()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Enable cheat detection to freeze growth
            _mockInventory.IsCheatDetected = true;

            // Tick clock
            SimulateClockTicks(service, 5);
            var slot = service.GetSlotAt(cell);

            Assert.AreEqual(0f, slot.growthTimeSec);
            Assert.AreEqual(FarmSlotState.Growing, slot.state);
            Assert.AreEqual(0, _stageChangedPub.Published.Count);
        }

        #endregion

        #region Nhóm Test Thu Hoạch (Harvesting Tests)

        [Test]
        public void Test_Harvest_Success_AddsProducts_And_Publishes_HarvestedEvent()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Ripen the crop
            SimulateClockTicks(service, 10);

            // Harvest
            bool success = service.TryHarvest(cell, out string product, out int amount);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.AreEqual("wheat_grain", product);
            Assert.AreEqual(2, amount);
            Assert.AreEqual(2, _mockInventory.GetItemCount("wheat_grain"));

            // Assert harvested event was fired
            Assert.AreEqual(1, _harvestedPub.Published.Count);
            var harvestedEvent = _harvestedPub.Published[0];
            Assert.AreEqual("wheat", harvestedEvent.EntityId);
            Assert.AreEqual(cell, harvestedEvent.Cell);
            Assert.AreEqual("wheat_grain", harvestedEvent.ProductItemId);
            Assert.AreEqual(2, harvestedEvent.Amount);
            Assert.AreEqual(FarmEntityType.Crop, harvestedEvent.EntityType);
        }

        [Test]
        public void Test_Harvest_Fails_If_NotRipe()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Tick only 5 seconds (not ripe yet)
            SimulateClockTicks(service, 5);

            bool success = service.TryHarvest(cell, out _, out _);
            Assert.IsFalse(success);
            Assert.AreEqual(0, _mockInventory.GetItemCount("wheat_grain"));
            Assert.AreEqual(0, _harvestedPub.Published.Count);
        }

        [Test]
        public void Test_Harvest_Animal_ResetsToEmptyAndUnfed()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "chicken");

            // Feed and ripen chicken
            _mockInventory.AddItem("wheat_grain", 1);
            service.TryFeed(cell);
            SimulateClockTicks(service, 15);

            // Harvest
            bool success = service.TryHarvest(cell, out string product, out int amount);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.AreEqual("egg", product);
            Assert.AreEqual(1, amount);
            Assert.AreEqual(1, _mockInventory.GetItemCount("egg"));

            // Verification of state reset
            Assert.IsNotNull(slot);
            Assert.AreEqual("chicken", slot.entityId);
            Assert.AreEqual(FarmSlotState.Empty, slot.state);
            Assert.IsFalse(slot.isFed);
            Assert.IsTrue(slot.isAdult);
        }

        [Test]
        public void Test_Harvest_Crop_AutoRestarts_If_MultiHarvest()
        {
            _sampleCrop.maxCycles = 2;
            _sampleCrop.autoRestart = true;

            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Ripen and harvest cycle 1
            SimulateClockTicks(service, 10);
            bool success = service.TryHarvest(cell, out _, out _);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.IsNotNull(slot);
            Assert.AreEqual("wheat", slot.entityId);
            Assert.AreEqual(FarmSlotState.Growing, slot.state, "Multi-harvest crop should restart in Growing state");
            Assert.AreEqual(1, slot.remainingHarvests);
            Assert.AreEqual(0f, slot.growthTimeSec);
        }

        [Test]
        public void Test_Harvest_Crop_ResetsToEmpty_If_MaxCyclesDepleted()
        {
            _sampleCrop.maxCycles = 1;
            _sampleCrop.autoRestart = true;

            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            // Ripen and harvest
            SimulateClockTicks(service, 10);
            bool success = service.TryHarvest(cell, out _, out _);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.IsNotNull(slot);
            Assert.IsNull(slot.entityId);
            Assert.AreEqual(FarmSlotState.Empty, slot.state);
        }

        #endregion

        #region Nhóm Test Tăng Tốc Vật Phẩm (TryApplyItem Tests)

        [Test]
        public void Test_ApplyItem_Booster_ReducesGrowthTime_And_Publishes_CaredEvent()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            var fertilizer = ScriptableObject.CreateInstance<BoosterItemDataSO>();
            fertilizer.name = "fertilizer";
            fertilizer.displayName = "Super Fertilizer";
            fertilizer.boostAmountSec = 5f;

            _mockInventory.AddItem("fertilizer", 1);

            bool success = service.TryApplyItem(cell, fertilizer);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.AreEqual(5f, slot.growthTimeSec);
            Assert.AreEqual(FarmSlotState.Growing, slot.state);
            Assert.AreEqual(0, _mockInventory.GetItemCount("fertilizer"));

            Assert.AreEqual(1, _caredPub.Published.Count);
            var caredEvent = _caredPub.Published[0];
            Assert.AreEqual("wheat", caredEvent.EntityId);
            Assert.AreEqual(cell, caredEvent.Cell);
            Assert.AreEqual(1, caredEvent.InputsApplied.Length);
            Assert.AreEqual("fertilizer", caredEvent.InputsApplied[0].item.ItemId);
        }

        [Test]
        public void Test_ApplyItem_Booster_RipensCropInstantly_IfBoostExceedsRemainingTime()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            var superFertilizer = ScriptableObject.CreateInstance<BoosterItemDataSO>();
            superFertilizer.name = "super_fertilizer";
            superFertilizer.displayName = "Ultra Accelerator";
            superFertilizer.boostAmountSec = 12f;

            _mockInventory.AddItem("super_fertilizer", 1);

            bool success = service.TryApplyItem(cell, superFertilizer);
            var slot = service.GetSlotAt(cell);

            Assert.IsTrue(success);
            Assert.AreEqual(10f, slot.growthTimeSec);
            Assert.AreEqual(FarmSlotState.Ripe, slot.state);

            Assert.AreEqual(1, _ripePub.Published.Count);
            Assert.AreEqual("wheat", _ripePub.Published[0].EntityId);
        }

        [Test]
        public void Test_ApplyItem_Fails_If_ItemNotBooster()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            var seed = ScriptableObject.CreateInstance<ItemDataSO>();
            seed.name = "wheat_grain";

            _mockInventory.AddItem("wheat_grain", 1);

            bool success = service.TryApplyItem(cell, seed);
            var slot = service.GetSlotAt(cell);

            Assert.IsFalse(success);
            Assert.AreEqual(0f, slot.growthTimeSec);
            Assert.AreEqual(1, _mockInventory.GetItemCount("wheat_grain"));
        }

        [Test]
        public void Test_ApplyItem_Fails_If_MissingInventoryItem()
        {
            var service = CreateFarmService();
            Vector3Int cell = new Vector3Int(1, 0, 1);
            service.TryPlant(cell, "wheat");

            var fertilizer = ScriptableObject.CreateInstance<BoosterItemDataSO>();
            fertilizer.name = "fertilizer";
            fertilizer.boostAmountSec = 5f;

            bool success = service.TryApplyItem(cell, fertilizer);
            var slot = service.GetSlotAt(cell);

            Assert.IsFalse(success);
            Assert.AreEqual(0f, slot.growthTimeSec);
        }

        #endregion
    }
}
