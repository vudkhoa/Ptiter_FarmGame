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

        private class StubDisposable : IDisposable
        {
            public void Dispose() {}
        }

        // Stub MessagePipe events publishers/subscribers
        private class StubPublisher<T> : IPublisher<T>
        {
            public void Publish(T message) {}
        }

        private class StubSubscriber<T> : ISubscriber<T>
        {
            public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
            {
                return new StubDisposable();
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
            
            // List to record mock published events for assertions
            public List<InventoryChangedPayload> PublishedEvents = new List<InventoryChangedPayload>();

            public int GetItemCount(string itemId) => Inv.TryGetValue(itemId, out var a) ? a : 0;
            
            public void AddItem(string itemId, int amount)
            {
                Inv[itemId] = GetItemCount(itemId) + amount;
                // Simulating PlayerDataHolder event dispatching
                PublishedEvents.Add(new InventoryChangedPayload(itemId, Inv[itemId], amount));
            }
            
            public bool RemoveItem(string itemId, int amount)
            {
                int count = GetItemCount(itemId);
                if (count < amount) return false;
                Inv[itemId] = count - amount;
                // Simulating PlayerDataHolder event dispatching for consumption (negative delta)
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
            var wheatGrainItem = ScriptableObject.CreateInstance<ItemDataSO>();
            wheatGrainItem.name = "wheat_grain";
            wheatGrainItem.displayName = "Wheat Grain";

            var eggItem = ScriptableObject.CreateInstance<ItemDataSO>();
            eggItem.name = "egg";
            eggItem.displayName = "Egg";

            // Setup Crop mockup: wheat grows in 10s
            _sampleCrop = ScriptableObject.CreateInstance<FarmEntityData>();
            _sampleCrop.name = "wheat";
            _sampleCrop.entityName = "Wheat";
            _sampleCrop.entityType = FarmEntityType.Crop;
            _sampleCrop.processTime = 10f;
            _sampleCrop.coinCost = 10;
            _sampleCrop.outputs = new OutputReward[] { new OutputReward { item = wheatGrainItem, amount = 2 } };
            _sampleCrop.autoRestart = true;
            _sampleCrop.maxCycles = 1;

            // Setup Animal mockup: chicken produces in 15s
            _sampleAnimal = ScriptableObject.CreateInstance<FarmEntityData>();
            _sampleAnimal.name = "chicken";
            _sampleAnimal.entityName = "Chicken";
            _sampleAnimal.entityType = FarmEntityType.Animal;
            _sampleAnimal.processTime = 15f;
            _sampleAnimal.coinCost = 50;
            _sampleAnimal.inputs = new InputRequirement[] { new InputRequirement { item = wheatGrainItem, amount = 1 } };
            _sampleAnimal.outputs = new OutputReward[] { new OutputReward { item = eggItem, amount = 1 } };
            _sampleAnimal.autoRestart = false;
            _sampleAnimal.maxCycles = -1;

            // Setup Database mockup
            _mockDatabase = ScriptableObject.CreateInstance<FarmDatabaseSO>();
            var allEntitiesField = typeof(FarmDatabaseSO).GetField("allEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            allEntitiesField.SetValue(_mockDatabase, new List<FarmEntityData> { _sampleCrop, _sampleAnimal });
            _mockDatabase.InitializeLookups();
        }

        [Test]
        public void Test_CropFsm_Transitions()
        {
            var savedSlots = new List<FarmSlotSaveData>();
            var farmService = new FarmService(
                _mockTimeProvider,
                _mockDatabase,
                _mockInventory,
                new StubSubscriber<ClockTickPayload>(),
                new StubPublisher<FarmSlotChangedPayload>()
            );
            farmService.Initialize(savedSlots, 0);

            Vector3Int cell = new Vector3Int(1, 0, 1);

            // 1. Initial state: Slot must be empty
            var slot = farmService.GetSlotAt(cell);
            Assert.IsNull(slot, "Initial soil tile slot should be empty/null");

            // 2. TryPlant (Empty -> Growing)
            bool plantResult = farmService.TryPlant(cell, "wheat");
            slot = farmService.GetSlotAt(cell);

            Assert.IsTrue(plantResult, "Planting crop should succeed");
            Assert.AreEqual(FarmSlotState.Growing, slot.state, "After planting, state must be Growing");
            Assert.AreEqual(990, _mockInventory.Coins, "Deducted 10 coins for seeds");
            Assert.AreEqual(0f, slot.growthTimeSec, "Initial growth progress must be 0s");

            // 3. Prevent harvesting while growing (Ensure state isn't skipped)
            bool earlyHarvest = farmService.TryHarvest(cell, out _, out _);
            Assert.IsFalse(earlyHarvest, "Should not be able to harvest growing crop");
            Assert.AreEqual(FarmSlotState.Growing, slot.state, "State must remain Growing after failed harvest");

            // 4. Tick time (Growing -> Ripe)
            var clockTickMethod = typeof(FarmService).GetMethod("OnClockTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            for (int i = 0; i < 10; i++)
            {
                _mockTimeProvider.CurrentTime = _mockTimeProvider.CurrentTime.AddSeconds(1);
                clockTickMethod.Invoke(farmService, new object[] { new ClockTickPayload(i + 1, _mockTimeProvider.UtcNow) });
            }

            Assert.AreEqual(FarmSlotState.Ripe, slot.state, "Crop should ripen after growTime (10s)");
            Assert.AreEqual(10f, slot.growthTimeSec, "Growth time should max at 10s");

            // Clear any setup events to verify only the harvest event
            _mockInventory.PublishedEvents.Clear();

            // 5. Harvest (Ripe -> Empty)
            bool harvestResult = farmService.TryHarvest(cell, out string product, out int amount);
            Assert.IsTrue(harvestResult, "Harvesting ripe crop should succeed");
            Assert.AreEqual(FarmSlotState.Empty, slot.state, "Slot should return to Empty after harvest");
            Assert.IsNull(slot.entityId, "Entity ID should be reset to null");
            Assert.AreEqual("wheat_grain", product, "Yield item should be wheat_grain");
            Assert.AreEqual(2, amount, "Yield amount should match harvestAmount");
            Assert.AreEqual(2, _mockInventory.GetItemCount("wheat_grain"), "Inventory should increase by yield amount");

            // Verification of update event dispatching:
            Assert.AreEqual(1, _mockInventory.PublishedEvents.Count, "Inventory mock should publish exactly 1 update event on harvest");
            var harvestEvent = _mockInventory.PublishedEvents[0];
            Assert.AreEqual("wheat_grain", harvestEvent.ItemId, "Event ItemId should be 'wheat_grain'");
            Assert.AreEqual(2, harvestEvent.NewAmount, "Event NewAmount should represent the updated total in inventory");
            Assert.AreEqual(2, harvestEvent.Delta, "Event Delta should be +2 (harvest amount)");
        }

        [Test]
        public void Test_AnimalFsm_FeedRequirement()
        {
            var savedSlots = new List<FarmSlotSaveData>();
            var farmService = new FarmService(
                _mockTimeProvider,
                _mockDatabase,
                _mockInventory,
                new StubSubscriber<ClockTickPayload>(),
                new StubPublisher<FarmSlotChangedPayload>()
            );
            farmService.Initialize(savedSlots, 0);

            Vector3Int cell = new Vector3Int(2, 0, 2);

            // 1. Buy Animal (Empty & Unfed)
            farmService.TryPlant(cell, "chicken");
            var slot = farmService.GetSlotAt(cell);

            Assert.AreEqual(FarmSlotState.Empty, slot.state, "New animal pen slot must be Empty");
            Assert.IsFalse(slot.isFed, "Animal should not be fed initially");
            Assert.AreEqual(950, _mockInventory.Coins, "Deducted 50 coins to purchase chicken");

            // 2. Prevent growing while unfed
            var clockTickMethod = typeof(FarmService).GetMethod("OnClockTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            _mockTimeProvider.CurrentTime = _mockTimeProvider.CurrentTime.AddSeconds(5);
            clockTickMethod.Invoke(farmService, new object[] { new ClockTickPayload(1, _mockTimeProvider.UtcNow) });

            Assert.AreEqual(0f, slot.growthTimeSec, "Animal should not grow/produce while unfed");

            // 3. TryFeed (Empty -> Growing)
            bool feedFail = farmService.TryFeed(cell);
            Assert.IsFalse(feedFail, "Feeding should fail if missing food items in inventory");

            // Add food and feed again
            _mockInventory.AddItem("wheat_grain", 1);
            _mockInventory.PublishedEvents.Clear(); // Clear the AddItem event
            
            bool feedSuccess = farmService.TryFeed(cell);

            Assert.IsTrue(feedSuccess, "Feeding should succeed when food is in inventory");
            Assert.IsTrue(slot.isFed, "isFed flag must be true");
            Assert.AreEqual(FarmSlotState.Growing, slot.state, "Animal state should change to Growing after feeding");
            Assert.AreEqual(0, _mockInventory.GetItemCount("wheat_grain"), "Inventory food should be consumed");

            // Verification of consumption event dispatching:
            Assert.AreEqual(1, _mockInventory.PublishedEvents.Count, "Inventory mock should publish exactly 1 event on feed consumption");
            var feedEvent = _mockInventory.PublishedEvents[0];
            Assert.AreEqual("wheat_grain", feedEvent.ItemId, "Event ItemId should be 'wheat_grain'");
            Assert.AreEqual(0, feedEvent.NewAmount, "Event NewAmount should represent the updated total (0)");
            Assert.AreEqual(-1, feedEvent.Delta, "Event Delta should be -1 (consumed food)");

            // 4. Production (Growing -> Ripe)
            for (int i = 0; i < 15; i++)
            {
                _mockTimeProvider.CurrentTime = _mockTimeProvider.CurrentTime.AddSeconds(1);
                clockTickMethod.Invoke(farmService, new object[] { new ClockTickPayload(i + 1, _mockTimeProvider.UtcNow) });
            }
            Assert.AreEqual(FarmSlotState.Ripe, slot.state, "Animal should ripen after productionTime (15s)");

            // Clear the feed event before harvesting product
            _mockInventory.PublishedEvents.Clear();

            // 5. Collect product (Ripe -> Empty & Unfed)
            bool collectResult = farmService.TryHarvest(cell, out string product, out int amount);
            Assert.IsTrue(collectResult, "Harvesting animal product should succeed");
            Assert.AreEqual(FarmSlotState.Empty, slot.state, "Slot should return to Empty (waiting for next feed)");
            Assert.IsFalse(slot.isFed, "isFed should be reset to false");
            Assert.AreEqual("egg", product);
            Assert.AreEqual(1, amount);
            Assert.AreEqual(1, _mockInventory.GetItemCount("egg"), "Inventory should receive product");

            // Verification of product harvest event dispatching:
            Assert.AreEqual(1, _mockInventory.PublishedEvents.Count, "Inventory mock should publish exactly 1 update event on product collect");
            var collectEvent = _mockInventory.PublishedEvents[0];
            Assert.AreEqual("egg", collectEvent.ItemId, "Event ItemId should be 'egg'");
            Assert.AreEqual(1, collectEvent.NewAmount, "Event NewAmount should be 1");
            Assert.AreEqual(1, collectEvent.Delta, "Event Delta should be +1");
        }
    }
}
