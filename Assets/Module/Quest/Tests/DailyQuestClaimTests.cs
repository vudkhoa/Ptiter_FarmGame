using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Core.Module.Quest.Tests
{
    public sealed class DailyQuestClaimTests
    {
        [Test]
        public void CompletedTask_RemainsVisibleUntilPlayerClaimsIt()
        {
            TestContext context = CreateContext();
            try
            {
                context.Service.EnsureInitializedAsync().GetAwaiter().GetResult();

                DailyQuestViewState state = context.Service.GetViewState();
                Assert.AreEqual(1, state.Tasks.Count);
                Assert.IsTrue(state.Tasks[0].IsCompleted);
                Assert.AreEqual(0, context.Rewards.GrantCalls);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ClaimTask_StagesReward_HidesTask_AndKeepsMilestonePoints()
        {
            TestContext context = CreateContext();
            try
            {
                context.Service.EnsureInitializedAsync().GetAwaiter().GetResult();
                string runtimeId = context.Service.GetViewState().Tasks[0].RuntimeId;

                bool claimed = context.Service
                    .ClaimTaskAsync(runtimeId)
                    .GetAwaiter()
                    .GetResult();

                DailyQuestViewState state = context.Service.GetViewState();
                Assert.IsTrue(claimed);
                Assert.AreEqual(1, context.Repository.StageCalls);
                Assert.AreEqual(1, context.Rewards.GrantCalls);
                Assert.AreEqual(0, state.Tasks.Count);
                Assert.AreEqual(25, state.TotalPoints);
                Assert.IsTrue(context.Repository.Persisted.tasks[0].rewardQueued);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void FailedStage_LeavesCompletedTaskVisibleAndClaimable()
        {
            TestContext context = CreateContext();
            context.Repository.FailStage = true;
            try
            {
                context.Service.EnsureInitializedAsync().GetAwaiter().GetResult();
                string runtimeId = context.Service.GetViewState().Tasks[0].RuntimeId;

                bool claimed = context.Service
                    .ClaimTaskAsync(runtimeId)
                    .GetAwaiter()
                    .GetResult();

                DailyQuestViewState state = context.Service.GetViewState();
                Assert.IsFalse(claimed);
                Assert.AreEqual(1, state.Tasks.Count);
                Assert.IsTrue(state.Tasks[0].IsCompleted);
                Assert.AreEqual(0, context.Rewards.GrantCalls);
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext()
        {
            const string questId = "daily-test";
            const string runtimeId = "daily:test:daily-test";

            QuestDefinitionSO definition =
                ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.questId = questId;
            definition.questName = "Test daily quest";
            definition.objectives = new List<QuestObjectiveData>
            {
                new QuestObjectiveData
                {
                    objectiveId = "objective",
                    objectiveType = QuestObjectiveType.ActionCount,
                    eventType = QuestEventType.FarmPlanted,
                    targetScope = QuestTargetScope.Any,
                    requiredAmount = 1
                }
            };

            DailyQuestSetSO set = ScriptableObject.CreateInstance<DailyQuestSetSO>();
            set.setId = "test-set";
            set.quests.Add(new DailyQuestEntry
            {
                quest = definition,
                points = 25,
                coinReward = 100
            });
            DailyQuestScheduleSO schedule =
                ScriptableObject.CreateInstance<DailyQuestScheduleSO>();
            schedule.sets.Add(set);

            QuestCatalogSO catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.quests.Add(definition);
            catalog.dailySchedule = schedule;

            var completedSnapshot = new QuestRuntimeSnapshot
            {
                runtimeId = runtimeId,
                questDefinitionId = questId,
                status = QuestStatus.Completed,
                objectives = new List<QuestObjectiveProgressSnapshot>
                {
                    new QuestObjectiveProgressSnapshot
                    {
                        objectiveId = "objective",
                        currentAmount = 1,
                        isCompleted = true
                    }
                }
            };
            var saved = new DailyQuestSaveData
            {
                dayKey = schedule.GetDayKey(DateTime.UtcNow),
                setId = set.setId,
                tasks = new List<DailyQuestTaskSaveData>
                {
                    new DailyQuestTaskSaveData
                    {
                        runtimeId = runtimeId,
                        questDefinitionId = questId,
                        points = 25,
                        coinReward = 100,
                        quest = completedSnapshot
                    }
                }
            };

            var progressPublisher = new NullPublisher<QuestProgressChangedPayload>();
            var completedPublisher = new NullPublisher<QuestCompletedPayload>();
            var questService = new QuestService(
                catalog,
                new QuestObjectiveRuleRegistry(new IQuestObjectiveRule[]
                {
                    new StateReachedObjectiveRule(new QuestProgressApplier()),
                    new ActionCountObjectiveRule(new QuestProgressApplier()),
                    new ItemAmountObjectiveRule(new QuestProgressApplier())
                }),
                new QuestCompletionEvaluator(),
                new NullPublisher<QuestAcceptedPayload>(),
                progressPublisher,
                completedPublisher);
            var repository = new FakeRepository(saved);
            var rewards = new FakeRewardService();
            var service = new DailyQuestService(
                catalog,
                questService,
                repository,
                rewards,
                new EmptySubscriber<QuestProgressChangedPayload>(),
                new EmptySubscriber<QuestCompletedPayload>(),
                new EmptySubscriber<Core.Module.Time.ClockTickPayload>(),
                new NullPublisher<DailyQuestStateChangedPayload>(),
                new NullPublisher<QuestRewardGrantedPayload>());
            return new TestContext(
                service, repository, rewards, catalog, schedule, set, definition);
        }

        private sealed class TestContext : IDisposable
        {
            public readonly DailyQuestService Service;
            public readonly FakeRepository Repository;
            public readonly FakeRewardService Rewards;
            private readonly UnityEngine.Object[] _assets;

            public TestContext(
                DailyQuestService service,
                FakeRepository repository,
                FakeRewardService rewards,
                params UnityEngine.Object[] assets)
            {
                Service = service;
                Repository = repository;
                Rewards = rewards;
                _assets = assets;
            }

            public void Dispose()
            {
                Service.Dispose();
                foreach (UnityEngine.Object asset in _assets)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private sealed class FakeRepository : IDailyQuestRepository
        {
            public bool IsLoaded => true;
            public bool FailStage { get; set; }
            public int StageCalls { get; private set; }
            public DailyQuestSaveData Persisted { get; private set; }
            private readonly List<PendingQuestRewardSaveData> _pending =
                new List<PendingQuestRewardSaveData>();

            public FakeRepository(DailyQuestSaveData saved)
            {
                Persisted = Clone(saved);
            }

            public UniTask WaitUntilLoadedAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public DailyQuestSaveData LoadDailyQuest() => Clone(Persisted);

            public IReadOnlyList<PendingQuestRewardSaveData>
                LoadPendingQuestRewards() => _pending;

            public UniTask<bool> SaveDailyQuestAsync(
                DailyQuestSaveData data,
                bool immediate,
                CancellationToken cancellationToken)
            {
                Persisted = Clone(data);
                return UniTask.FromResult(true);
            }

            public UniTask<bool> StageQuestRewardAsync(
                DailyQuestSaveData data,
                PendingQuestRewardSaveData pendingReward,
                CancellationToken cancellationToken)
            {
                StageCalls++;
                if (FailStage) return UniTask.FromResult(false);
                Persisted = Clone(data);
                _pending.Add(Clone(pendingReward));
                return UniTask.FromResult(true);
            }

            public UniTask<bool> CompletePendingQuestRewardAsync(
                string transactionId,
                CancellationToken cancellationToken)
            {
                _pending.RemoveAll(item => item.transactionId == transactionId);
                return UniTask.FromResult(true);
            }

            private static T Clone<T>(T value) where T : class
            {
                return value == null
                    ? null
                    : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
            }
        }

        private sealed class FakeRewardService : IQuestRewardService
        {
            public int GrantCalls { get; private set; }

            public UniTask<QuestRewardGrantResult> GrantPendingCoinsAsync(
                string transactionId,
                CancellationToken cancellationToken)
            {
                GrantCalls++;
                return UniTask.FromResult(new QuestRewardGrantResult(
                    true, false, 100, null));
            }
        }

        private sealed class NullPublisher<T> : IPublisher<T>
        {
            public void Publish(T message) { }
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

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
