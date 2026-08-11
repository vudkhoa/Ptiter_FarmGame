using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Core.Module.Quest.Tests
{
    [TestFixture]
    public sealed class FoodRecipeTests
    {
        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public readonly List<T> Messages = new List<T>();
            public void Publish(T message) => Messages.Add(message);
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class EmptySubscriber<T> : ISubscriber<T>
        {
            public IDisposable Subscribe(
                IMessageHandler<T> handler,
                params MessageHandlerFilter<T>[] filters)
            {
                return new EmptyDisposable();
            }
        }

        private sealed class FakeStarWallet : IStarWalletService
        {
            public bool IsReady => true;
            public int Stars { get; set; }
            public int PurchaseCalls { get; private set; }
            public readonly HashSet<string> Unlocked = new HashSet<string>();

            public UniTask EnsureInitializedAsync(
                CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public bool IsStarUnlockPurchased(string unlockId)
            {
                return Unlocked.Contains(unlockId);
            }

            public UniTask<StarUnlockPurchaseResult>
                TryPurchaseStarUnlockAsync(
                    string unlockId,
                    int cost,
                    CancellationToken cancellationToken = default)
            {
                PurchaseCalls++;
                if (Stars < cost)
                    return UniTask.FromResult(new StarUnlockPurchaseResult(
                        StarUnlockPurchaseState.InsufficientStars, Stars));
                Stars -= cost;
                Unlocked.Add(unlockId);
                return UniTask.FromResult(new StarUnlockPurchaseResult(
                    StarUnlockPurchaseState.Success, Stars));
            }
        }

        private sealed class FakeProgressRepository : IProgressQuestRepository
        {
            public bool IsLoaded => true;
            public bool FailNextSave { get; set; }
            public ProgressQuestSaveData Persisted { get; private set; }

            public FakeProgressRepository(ProgressQuestSaveData initial)
            {
                Persisted = Clone(initial);
            }

            public UniTask WaitUntilLoadedAsync(
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public ProgressQuestSaveData LoadProgressQuest()
            {
                return Clone(Persisted);
            }

            public UniTask<bool> SaveProgressQuestAsync(
                ProgressQuestSaveData data,
                CancellationToken cancellationToken)
            {
                if (FailNextSave)
                {
                    FailNextSave = false;
                    return UniTask.FromResult(false);
                }
                Persisted = Clone(data);
                return UniTask.FromResult(true);
            }

            private static ProgressQuestSaveData Clone(
                ProgressQuestSaveData source)
            {
                return new ProgressQuestSaveData
                {
                    accumulatedCoins = source.accumulatedCoins,
                    stars = source.stars,
                    claimedMilestoneIds = new List<string>(
                        source.claimedMilestoneIds ?? new List<string>()),
                    unlockedRecipeIds = new List<string>(
                        source.unlockedRecipeIds ?? new List<string>())
                };
            }
        }

        [Test]
        public void NewUser_SeesOnlyObscuredLockedRecipeData()
        {
            FakeStarWallet wallet = new FakeStarWallet { Stars = 80 };
            FoodRecipeService service = CreateFoodService(wallet);

            FoodRecipeViewState state = service.GetViewState();

            Assert.AreEqual(FoodRecipeAccessState.Locked,
                state.Recipes[0].AccessState);
            Assert.AreEqual(FoodRecipeAccessState.PrerequisiteLocked,
                state.Recipes[1].AccessState);
            Assert.IsEmpty(state.Recipes[0].DisplayName);
            Assert.IsEmpty(state.Recipes[0].MockIngredients);
            Assert.IsEmpty(state.Recipes[1].DisplayName);
        }

        [Test]
        public void LowerRecipe_CannotUnlockBeforeUpperRecipe()
        {
            FakeStarWallet wallet = new FakeStarWallet { Stars = 80 };
            FoodRecipeService service = CreateFoodService(wallet);

            FoodRecipeUnlockResult result = service.TryUnlockAsync("nem_ran")
                .GetAwaiter().GetResult();

            Assert.AreEqual(
                FoodRecipeUnlockResultCode.PrerequisiteLocked, result.Code);
            Assert.AreEqual(0, wallet.PurchaseCalls);
            Assert.AreEqual(80, wallet.Stars);
        }

        [Test]
        public void SequentialUnlock_SpendsThirtyThenFiftyStars()
        {
            FakeStarWallet wallet = new FakeStarWallet { Stars = 80 };
            FoodRecipeService service = CreateFoodService(wallet);

            FoodRecipeUnlockResult first =
                service.TryUnlockAsync("banh_mi_heo_quay")
                    .GetAwaiter().GetResult();
            FoodRecipeUnlockResult second = service.TryUnlockAsync("nem_ran")
                .GetAwaiter().GetResult();
            FoodRecipeViewState state = service.GetViewState();

            Assert.AreEqual(FoodRecipeUnlockResultCode.Success, first.Code);
            Assert.AreEqual(FoodRecipeUnlockResultCode.Success, second.Code);
            Assert.AreEqual(0, wallet.Stars);
            Assert.AreEqual("BÁNH MÌ HEO QUAY", state.Recipes[0].DisplayName);
            Assert.AreEqual("NEM RÁN", state.Recipes[1].DisplayName);
        }

        [Test]
        public void ProgressWallet_RollsBackStarsAndUnlock_WhenSaveFails()
        {
            QuestCatalogSO catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.progressConfig =
                ScriptableObject.CreateInstance<ProgressQuestConfigSO>();
            var repository = new FakeProgressRepository(
                new ProgressQuestSaveData { stars = 80 });
            var statePublisher =
                new RecordingPublisher<ProgressQuestStateChangedPayload>();
            var rewardPublisher =
                new RecordingPublisher<ProgressRewardClaimedPayload>();
            var service = new ProgressQuestService(
                catalog,
                repository,
                new EmptySubscriber<ProgressCoinsEarnedPayload>(),
                statePublisher,
                rewardPublisher);

            service.EnsureInitializedAsync().GetAwaiter().GetResult();
            StarUnlockPurchaseResult first = service
                .TryPurchaseStarUnlockAsync("banh_mi_heo_quay", 30)
                .GetAwaiter().GetResult();
            repository.FailNextSave = true;
            StarUnlockPurchaseResult failed = service
                .TryPurchaseStarUnlockAsync("nem_ran", 50)
                .GetAwaiter().GetResult();

            Assert.AreEqual(StarUnlockPurchaseState.Success, first.State);
            Assert.AreEqual(StarUnlockPurchaseState.SaveFailed, failed.State);
            Assert.AreEqual(50, service.Stars);
            Assert.IsTrue(service.IsStarUnlockPurchased("banh_mi_heo_quay"));
            Assert.IsFalse(service.IsStarUnlockPurchased("nem_ran"));
            CollectionAssert.AreEquivalent(
                new[] { "banh_mi_heo_quay" },
                repository.Persisted.unlockedRecipeIds);
            service.Dispose();
        }

        private static FoodRecipeService CreateFoodService(
            FakeStarWallet wallet)
        {
            FoodRecipeConfigSO config =
                ScriptableObject.CreateInstance<FoodRecipeConfigSO>();
            config.recipes = new List<FoodRecipeDefinition>
            {
                new FoodRecipeDefinition
                {
                    recipeId = "banh_mi_heo_quay",
                    displayName = "BÁNH MÌ HEO QUAY",
                    mockIngredients = "mock",
                    starCost = 30
                },
                new FoodRecipeDefinition
                {
                    recipeId = "nem_ran",
                    displayName = "NEM RÁN",
                    mockIngredients = "mock",
                    starCost = 50,
                    prerequisiteRecipeId = "banh_mi_heo_quay"
                }
            };
            QuestCatalogSO catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.foodRecipeConfig = config;
            return new FoodRecipeService(
                catalog,
                wallet,
                new RecordingPublisher<FoodRecipeStateChangedPayload>(),
                new RecordingPublisher<FoodRecipeUnlockedPayload>());
        }
    }
}
