using System;
using System.Collections.Generic;
using Core.Module.Quest.Cooking;
using Core.Module.Storage;
using Core.Module.Time;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Core.Module.Quest.Tests.Cooking
{
    [TestFixture]
    public sealed class CookingServiceTests
    {
        private readonly List<CookingService> _services =
            new List<CookingService>();
        private readonly List<ScriptableObject> _assets =
            new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _services.Count; i++)
                _services[i]?.Dispose();
            _services.Clear();

            for (int i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                    UnityEngine.Object.DestroyImmediate(_assets[i]);
            }
            _assets.Clear();
        }

        [Test]
        public void RecipeState_UsesLimitingIngredientForMaxCraftable()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 5);
            storage.Set("wheat_grain", 3);
            CookingService service = CreateService(storage: storage);

            CookingRecipeState state = service.GetRecipeState(
                "banh_mi_heo_quay");

            Assert.That(state.MaxCraftable, Is.EqualTo(2));
            Assert.That(state.Ingredients[0].RequiredAmount, Is.EqualTo(2));
            Assert.That(state.Ingredients[1].RequiredAmount, Is.EqualTo(1));
        }

        [Test]
        public void StartCooking_ConsumesScaledIngredientsAndPersistsJob()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 6);
            storage.Set("wheat_grain", 3);
            FakeRepository repository = new FakeRepository();
            CookingService service = CreateService(storage, repository);

            CookingStartResult result = service.TryStartCooking(
                "banh_mi_heo_quay",
                3);

            Assert.That(result.Code, Is.EqualTo(CookingStartResultCode.Success));
            Assert.That(storage.GetItemCount("pork_belly"), Is.Zero);
            Assert.That(storage.GetItemCount("wheat_grain"), Is.Zero);
            Assert.That(repository.SaveCalls, Is.EqualTo(1));
            Assert.That(repository.Job.quantity, Is.EqualTo(3));
            Assert.That(repository.Job.totalSeconds, Is.EqualTo(60));
            Assert.That(repository.Job.outputAmount, Is.EqualTo(3));
        }

        [Test]
        public void StartCooking_InsufficientIngredientsDoesNotMutateState()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 1);
            storage.Set("wheat_grain", 5);
            FakeRepository repository = new FakeRepository();
            CookingService service = CreateService(storage, repository);

            CookingStartResult result = service.TryStartCooking(
                "banh_mi_heo_quay",
                1);

            Assert.That(
                result.Code,
                Is.EqualTo(CookingStartResultCode.InsufficientIngredients));
            Assert.That(storage.GetItemCount("pork_belly"), Is.EqualTo(1));
            Assert.That(storage.GetItemCount("wheat_grain"), Is.EqualTo(5));
            Assert.That(repository.SaveCalls, Is.Zero);
        }

        [Test]
        public void StartCooking_SecondRemovalFailureRollsBackFirstIngredient()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 2);
            storage.Set("wheat_grain", 1);
            storage.FailRemoveItemId = "wheat_grain";
            FakeRepository repository = new FakeRepository();
            CookingService service = CreateService(storage, repository);

            CookingStartResult result = service.TryStartCooking(
                "banh_mi_heo_quay",
                1);

            Assert.That(
                result.Code,
                Is.EqualTo(CookingStartResultCode.ConsumeFailed));
            Assert.That(storage.GetItemCount("pork_belly"), Is.EqualTo(2));
            Assert.That(storage.GetItemCount("wheat_grain"), Is.EqualTo(1));
            Assert.That(repository.SaveCalls, Is.Zero);
        }

        [Test]
        public void StartCooking_RejectsSecondGlobalJob()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 8);
            storage.Set("wheat_grain", 4);
            CookingService service = CreateService(storage: storage);

            CookingStartResult first = service.TryStartCooking(
                "banh_mi_heo_quay",
                1);
            CookingStartResult second = service.TryStartCooking(
                "banh_mi_heo_quay",
                1);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.Code, Is.EqualTo(CookingStartResultCode.Busy));
            Assert.That(storage.GetItemCount("pork_belly"), Is.EqualTo(6));
            Assert.That(storage.GetItemCount("wheat_grain"), Is.EqualTo(3));
        }

        [Test]
        public void Refresh_WhenUtcDeadlinePassedPublishesPendingCompletion()
        {
            FakeStorage storage = new FakeStorage();
            storage.Set("pork_belly", 6);
            storage.Set("wheat_grain", 3);
            FakeRepository repository = new FakeRepository();
            FakeTimeProvider time = new FakeTimeProvider();
            RecordingPublisher<CookingCompletedPayload> completed =
                new RecordingPublisher<CookingCompletedPayload>();
            CookingService service = CreateService(
                storage,
                repository,
                time,
                completed);
            service.TryStartCooking("banh_mi_heo_quay", 3);

            time.UtcNowValue = time.UtcNowValue.AddSeconds(61);
            service.Refresh();

            Assert.That(repository.Job.completionPending, Is.True);
            Assert.That(completed.Messages.Count, Is.EqualTo(1));
            Assert.That(completed.Messages[0].Amount, Is.EqualTo(3));
            Assert.That(completed.Messages[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void Initialize_ResumesPersistedJobFromUtcEndTime()
        {
            FakeTimeProvider time = new FakeTimeProvider();
            FakeRepository repository = new FakeRepository
            {
                Job = new CookingJobSaveData
                {
                    transactionId = "resume-1",
                    recipeId = "banh_mi_heo_quay",
                    outputItemId = "banh_mi_heo_quay",
                    outputAmount = 2,
                    quantity = 2,
                    totalSeconds = 40,
                    endsAtUtcTicks = time.UtcNowValue.AddSeconds(12).Ticks
                }
            };
            CookingService service = CreateService(
                repository: repository,
                time: time);

            CookingRecipeState state = service.GetRecipeState(
                "banh_mi_heo_quay");

            Assert.That(state.IsCooking, Is.True);
            Assert.That(state.CookingQuantity, Is.EqualTo(2));
            Assert.That(state.RemainingSeconds, Is.EqualTo(12));
        }

        [Test]
        public void CompletionBridge_DuplicateEventGrantsOutputOnlyOnce()
        {
            FakeRepository repository = new FakeRepository
            {
                Job = new CookingJobSaveData
                {
                    transactionId = "txn-1",
                    recipeId = "banh_mi_heo_quay",
                    outputItemId = "banh_mi_heo_quay",
                    outputAmount = 3,
                    quantity = 3,
                    totalSeconds = 60,
                    endsAtUtcTicks = DateTime.UtcNow.Ticks,
                    completionPending = true
                }
            };
            DispatchingSubscriber<CookingCompletedPayload> completed =
                new DispatchingSubscriber<CookingCompletedPayload>();
            RecordingPublisher<CookingCompletionCommittedPayload> committed =
                new RecordingPublisher<CookingCompletionCommittedPayload>();
            using (CookingCompletionStorageBridge bridge =
                   new CookingCompletionStorageBridge(
                       repository,
                       completed,
                       committed))
            {
                CookingCompletedPayload payload = new CookingCompletedPayload(
                    "txn-1",
                    "banh_mi_heo_quay",
                    "banh_mi_heo_quay",
                    3,
                    3);
                completed.Publish(payload);
                completed.Publish(payload);
            }

            Assert.That(repository.GrantedAmount, Is.EqualTo(3));
            Assert.That(repository.CommittedTransactions.Count, Is.EqualTo(1));
            Assert.That(committed.Messages.Count, Is.EqualTo(2));
        }

        [Test]
        public void CompletionBridge_MismatchedOutputIsRejected()
        {
            FakeRepository repository = new FakeRepository
            {
                Job = new CookingJobSaveData
                {
                    transactionId = "txn-2",
                    recipeId = "banh_mi_heo_quay",
                    outputItemId = "banh_mi_heo_quay",
                    outputAmount = 1,
                    quantity = 1,
                    totalSeconds = 20,
                    endsAtUtcTicks = DateTime.UtcNow.Ticks,
                    completionPending = true
                }
            };
            DispatchingSubscriber<CookingCompletedPayload> completed =
                new DispatchingSubscriber<CookingCompletedPayload>();
            RecordingPublisher<CookingCompletionCommittedPayload> committed =
                new RecordingPublisher<CookingCompletionCommittedPayload>();
            using (CookingCompletionStorageBridge bridge =
                   new CookingCompletionStorageBridge(
                       repository,
                       completed,
                       committed))
            {
                completed.Publish(new CookingCompletedPayload(
                    "txn-2",
                    "banh_mi_heo_quay",
                    "bonsai",
                    999,
                    1));
            }

            Assert.That(repository.GrantedAmount, Is.Zero);
            Assert.That(repository.Job, Is.Not.Null);
            Assert.That(committed.Messages, Is.Empty);
        }

        private CookingService CreateService(
            FakeStorage storage = null,
            FakeRepository repository = null,
            FakeTimeProvider time = null,
            RecordingPublisher<CookingCompletedPayload> completed = null)
        {
            CookingRecipeConfigSO config =
                ScriptableObject.CreateInstance<CookingRecipeConfigSO>();
            config.maxQuantity = 99;
            config.recipes.Add(new CookingRecipeDefinition
            {
                recipeId = "banh_mi_heo_quay",
                displayName = "Bánh Mì Heo Quay",
                secondsPerItem = 20,
                outputItemId = "banh_mi_heo_quay",
                outputAmountPerItem = 1,
                ingredients = new List<CookingIngredientDefinition>
                {
                    new CookingIngredientDefinition
                    {
                        itemId = "pork_belly",
                        displayName = "Ba Chỉ Heo",
                        amountPerItem = 2
                    },
                    new CookingIngredientDefinition
                    {
                        itemId = "wheat_grain",
                        displayName = "Lúa Mì",
                        amountPerItem = 1
                    }
                }
            });
            QuestCatalogSO catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.cookingRecipeConfig = config;
            _assets.Add(config);
            _assets.Add(catalog);

            CookingService service = new CookingService(
                catalog,
                storage ?? new FakeStorage(),
                repository ?? new FakeRepository(),
                time ?? new FakeTimeProvider(),
                new RecordingPublisher<CookingStateChangedPayload>(),
                completed ?? new RecordingPublisher<CookingCompletedPayload>(),
                new DispatchingSubscriber<CookingCompletionCommittedPayload>(),
                new DispatchingSubscriber<InventoryChangedPayload>());
            service.Initialize();
            _services.Add(service);
            return service;
        }

        private sealed class FakeStorage : IStorageService
        {
            private readonly Dictionary<string, int> _items =
                new Dictionary<string, int>(StringComparer.Ordinal);

            public string FailRemoveItemId { get; set; }
            public int Coins { get; set; }
            public bool IsCheatDetected { get; set; }
            public long LastSaveUtcTicks => 0;

            public void Set(string itemId, int amount)
            {
                _items[itemId] = amount;
            }

            public int GetItemCount(string itemId)
            {
                int amount;
                return _items.TryGetValue(itemId, out amount) ? amount : 0;
            }

            public void AddItem(string itemId, int amount)
            {
                _items[itemId] = GetItemCount(itemId) + amount;
            }

            public bool RemoveItem(string itemId, int amount)
            {
                if (string.Equals(
                        itemId,
                        FailRemoveItemId,
                        StringComparison.Ordinal))
                    return false;
                int current = GetItemCount(itemId);
                if (amount <= 0 || current < amount) return false;
                _items[itemId] = current - amount;
                return true;
            }

            public void Save() { }
        }

        private sealed class FakeRepository : ICookingJobRepository
        {
            public CookingJobSaveData Job { get; set; }
            public int SaveCalls { get; private set; }
            public int GrantedAmount { get; private set; }
            public HashSet<string> CommittedTransactions { get; } =
                new HashSet<string>(StringComparer.Ordinal);

            public CookingJobSaveData LoadActiveCookingJob()
            {
                return Clone(Job);
            }

            public bool SaveActiveCookingJob(CookingJobSaveData job)
            {
                SaveCalls++;
                Job = Clone(job);
                return true;
            }

            public bool TryCommitCookingCompletion(
                CookingCompletedPayload payload)
            {
                if (CommittedTransactions.Contains(payload.TransactionId))
                    return true;
                if (Job == null ||
                    Job.transactionId != payload.TransactionId ||
                    Job.recipeId != payload.RecipeId ||
                    Job.outputItemId != payload.OutputItemId ||
                    Job.outputAmount != payload.Amount ||
                    Job.quantity != payload.Quantity)
                    return false;

                CommittedTransactions.Add(payload.TransactionId);
                GrantedAmount += payload.Amount;
                Job = null;
                return true;
            }

            private static CookingJobSaveData Clone(CookingJobSaveData source)
            {
                if (source == null) return null;
                return new CookingJobSaveData
                {
                    transactionId = source.transactionId,
                    recipeId = source.recipeId,
                    outputItemId = source.outputItemId,
                    outputAmount = source.outputAmount,
                    quantity = source.quantity,
                    totalSeconds = source.totalSeconds,
                    endsAtUtcTicks = source.endsAtUtcTicks,
                    completionPending = source.completionPending
                };
            }
        }

        private sealed class FakeTimeProvider : IServerTimeProvider
        {
            public DateTime UtcNowValue { get; set; } =
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => UtcNowValue;
            public bool IsSynced => true;
            public TimeSpan Offset => TimeSpan.Zero;
            public DateTime LastSyncedAt => UtcNowValue;
        }

        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public readonly List<T> Messages = new List<T>();
            public void Publish(T message) => Messages.Add(message);
        }

        private sealed class DispatchingSubscriber<T> : ISubscriber<T>
        {
            private IMessageHandler<T> _handler;

            public IDisposable Subscribe(
                IMessageHandler<T> handler,
                params MessageHandlerFilter<T>[] filters)
            {
                _handler = handler;
                return new EmptyDisposable();
            }

            public void Publish(T message)
            {
                _handler?.Handle(message);
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
