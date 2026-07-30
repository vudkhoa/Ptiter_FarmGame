using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Time;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Quest
{
    public sealed class DailyQuestService : IDailyQuestService, IDisposable
    {
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5)
        };
        private static readonly TimeSpan ServerTimeWaitTimeout =
            TimeSpan.FromSeconds(8);

        private readonly QuestCatalogSO _catalog;
        private readonly IQuestService _questService;
        private readonly IDailyQuestRepository _repository;
        private readonly IQuestRewardService _rewardService;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IPublisher<DailyQuestStateChangedPayload> _statePublisher;
        private readonly IPublisher<QuestRewardGrantedPayload> _rewardPublisher;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
        private readonly HashSet<string> _pendingTransactions = new HashSet<string>();
        private readonly HashSet<string> _retryingTransactions = new HashSet<string>();

        private DailyQuestSaveData _state;
        private bool _initializing;
        private bool _resetting;

        public bool IsReady { get; private set; }

        public DailyQuestService(
            QuestCatalogSO catalog,
            IQuestService questService,
            IDailyQuestRepository repository,
            IQuestRewardService rewardService,
            IServerTimeProvider timeProvider,
            ISubscriber<QuestProgressChangedPayload> progressSubscriber,
            ISubscriber<QuestCompletedPayload> completedSubscriber,
            ISubscriber<ClockTickPayload> tickSubscriber,
            ISubscriber<ServerTimeSyncedPayload> syncedSubscriber,
            IPublisher<DailyQuestStateChangedPayload> statePublisher,
            IPublisher<QuestRewardGrantedPayload> rewardPublisher)
        {
            _catalog = catalog;
            _questService = questService;
            _repository = repository;
            _rewardService = rewardService;
            _timeProvider = timeProvider;
            _statePublisher = statePublisher;
            _rewardPublisher = rewardPublisher;

            _subscriptions.Add(progressSubscriber.Subscribe(OnQuestProgressChanged));
            _subscriptions.Add(completedSubscriber.Subscribe(OnQuestCompleted));
            _subscriptions.Add(tickSubscriber.Subscribe(OnClockTick));
            _subscriptions.Add(syncedSubscriber.Subscribe(_ =>
                EnsureInitializedAsync(_lifetimeCts.Token).Forget()));
        }

        public async UniTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (IsReady || _initializing) return;
            _initializing = true;
            try
            {
                await _repository.WaitUntilLoadedAsync(cancellationToken);
                DateTime timeWaitDeadline =
                    DateTime.UtcNow + ServerTimeWaitTimeout;
                while (!_timeProvider.IsSynced &&
                       DateTime.UtcNow < timeWaitDeadline)
                {
                    await UniTask.Delay(
                        TimeSpan.FromMilliseconds(250),
                        cancellationToken: cancellationToken);
                }
                if (!_timeProvider.IsSynced)
                {
                    Debug.LogWarning(
                        "[DailyQuest] Server time is unavailable; " +
                        "using the current UTC clock for this session.");
                }

                DailyQuestScheduleSO schedule = _catalog != null ? _catalog.dailySchedule : null;
                if (schedule == null)
                {
                    Debug.LogError("[DailyQuest] QuestCatalogSO has no DailyQuestScheduleSO.");
                    return;
                }

                DateTime now = _timeProvider.UtcNow;
                string currentDay = schedule.GetDayKey(now);
                DailyQuestSaveData saved = _repository.LoadDailyQuest();
                if (saved == null ||
                    saved.dayKey != currentDay ||
                    schedule.GetSetById(saved.setId) == null)
                {
                    saved = BuildNewDay(schedule, now);
                    if (saved == null) return;
                    await _repository.SaveDailyQuestAsync(saved, true, cancellationToken);
                }

                _state = saved;
                LoadPendingTransactions();
                ActivateSavedTasks();
                IsReady = true;
                PublishStateChanged();
                ReconcileCompletedTasks();
                ReconcilePendingRewardsAsync().Forget();
            }
            catch (OperationCanceledException)
            {
                // Normal scene teardown.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _initializing = false;
            }
        }

        public DailyQuestViewState GetViewState()
        {
            DailyQuestScheduleSO schedule = _catalog != null ? _catalog.dailySchedule : null;
            if (!IsReady || _state == null || schedule == null)
            {
                return new DailyQuestViewState
                {
                    IsReady = false,
                    LockedReason = _timeProvider.IsSynced
                        ? "Daily quests are loading..."
                        : "Synchronizing server time...",
                    Tasks = Array.Empty<DailyQuestTaskViewData>(),
                    Milestones = Array.Empty<DailyMilestoneViewData>()
                };
            }

            int totalPoints = CalculateTotalPoints();
            var taskViews = new List<DailyQuestTaskViewData>(_state.tasks.Count);
            for (int i = 0; i < _state.tasks.Count; i++)
            {
                DailyQuestTaskSaveData task = _state.tasks[i];
                QuestDefinitionSO definition = _catalog.GetQuestById(task.questDefinitionId);
                QuestRuntimeState runtime = _questService.GetQuestState(task.runtimeId);
                int current = 0;
                int required = 0;
                if (definition?.objectives != null && runtime != null)
                {
                    for (int objectiveIndex = 0;
                         objectiveIndex < definition.objectives.Count;
                         objectiveIndex++)
                    {
                        QuestObjectiveData objective = definition.objectives[objectiveIndex];
                        if (objective == null ||
                            !runtime.TryGetProgress(objective.objectiveId, out QuestObjectiveProgress progress))
                            continue;
                        current += progress.currentAmount;
                        required += Math.Max(1, objective.requiredAmount);
                    }
                }

                string transactionId = CreateTaskTransactionId(_state.dayKey, task.runtimeId);
                taskViews.Add(new DailyQuestTaskViewData
                {
                    RuntimeId = task.runtimeId,
                    DefinitionId = task.questDefinitionId,
                    Title = definition != null ? definition.questName : task.questDefinitionId,
                    Description = definition != null ? definition.description : string.Empty,
                    Icon = definition != null ? definition.icon : null,
                    CurrentAmount = current,
                    RequiredAmount = Math.Max(1, required),
                    CoinReward = task.coinReward,
                    IsCompleted = runtime?.Status == QuestStatus.Completed,
                    IsRewardPending = _pendingTransactions.Contains(transactionId)
                });
            }

            var milestoneViews = new List<DailyMilestoneViewData>(_state.milestones.Count);
            for (int i = 0; i < _state.milestones.Count; i++)
            {
                DailyMilestoneSaveData milestone = _state.milestones[i];
                string transactionId = CreateMilestoneTransactionId(
                    _state.dayKey, milestone.milestoneId);
                DailyMilestoneClaimState claimState;
                if (_pendingTransactions.Contains(transactionId))
                    claimState = DailyMilestoneClaimState.ClaimPending;
                else if (milestone.claimed)
                    claimState = DailyMilestoneClaimState.Claimed;
                else if (totalPoints >= milestone.requiredPoints)
                    claimState = DailyMilestoneClaimState.Claimable;
                else
                    claimState = DailyMilestoneClaimState.Locked;

                milestoneViews.Add(new DailyMilestoneViewData
                {
                    MilestoneId = milestone.milestoneId,
                    RequiredPoints = milestone.requiredPoints,
                    CoinReward = milestone.coinReward,
                    ClaimState = claimState
                });
            }

            return new DailyQuestViewState
            {
                IsReady = true,
                DayKey = _state.dayKey,
                TotalPoints = totalPoints,
                TimeUntilReset = MaxZero(schedule.GetNextResetUtc(_timeProvider.UtcNow) -
                                         _timeProvider.UtcNow),
                Tasks = taskViews,
                Milestones = milestoneViews
            };
        }

        public async UniTask<bool> ClaimMilestoneAsync(
            string milestoneId,
            CancellationToken cancellationToken = default)
        {
            if (!IsReady || string.IsNullOrWhiteSpace(milestoneId)) return false;
            DailyMilestoneSaveData milestone = _state.milestones.Find(
                item => item != null && item.milestoneId == milestoneId);
            if (milestone == null || milestone.claimed ||
                CalculateTotalPoints() < milestone.requiredPoints)
                return false;

            string transactionId = CreateMilestoneTransactionId(_state.dayKey, milestoneId);
            milestone.claimed = true;
            var pending = new PendingQuestRewardSaveData
            {
                transactionId = transactionId,
                dayKey = _state.dayKey,
                sourceId = milestoneId,
                kind = DailyRewardKind.Milestone,
                coins = milestone.coinReward
            };

            bool staged = await _repository.StageQuestRewardAsync(
                _state, pending, cancellationToken);
            if (!staged)
            {
                milestone.claimed = false;
                return false;
            }

            _pendingTransactions.Add(transactionId);
            PublishStateChanged();
            bool granted = await TryGrantRewardAsync(pending, false, cancellationToken);
            if (!granted)
                StartRewardRetry(pending, false);
            return true;
        }

        private void OnQuestCompleted(QuestCompletedPayload payload)
        {
            if (!IsReady || _state?.tasks == null) return;
            DailyQuestTaskSaveData task = _state.tasks.Find(
                item => item != null && item.runtimeId == payload.RuntimeId);
            if (task == null || task.rewardQueued) return;

            task.quest = _questService.CreateSnapshot(task.runtimeId);
            task.rewardQueued = true;
            QueueTaskRewardAsync(task, false).Forget();
        }

        private void OnQuestProgressChanged(QuestProgressChangedPayload payload)
        {
            if (!IsReady || _state?.tasks == null) return;
            DailyQuestTaskSaveData task = _state.tasks.Find(
                item => item != null && item.runtimeId == payload.RuntimeId);
            if (task == null) return;
            task.quest = _questService.CreateSnapshot(task.runtimeId);
            _repository.SaveDailyQuestAsync(
                _state, false, _lifetimeCts.Token).Forget();
            PublishStateChanged();
        }

        private async UniTaskVoid QueueTaskRewardAsync(
            DailyQuestTaskSaveData task,
            bool reconciledAtStartup)
        {
            string transactionId = CreateTaskTransactionId(_state.dayKey, task.runtimeId);
            var pending = new PendingQuestRewardSaveData
            {
                transactionId = transactionId,
                dayKey = _state.dayKey,
                sourceId = task.runtimeId,
                kind = DailyRewardKind.Task,
                coins = task.coinReward
            };

            bool staged = await _repository.StageQuestRewardAsync(
                _state, pending, _lifetimeCts.Token);
            if (!staged)
            {
                task.rewardQueued = false;
                await RetryStageAndGrantAsync(task, pending, reconciledAtStartup);
                return;
            }

            _pendingTransactions.Add(transactionId);
            PublishStateChanged();
            bool granted = await TryGrantRewardAsync(
                pending, reconciledAtStartup, _lifetimeCts.Token);
            if (!granted)
                StartRewardRetry(pending, reconciledAtStartup);
        }

        private async UniTask RetryStageAndGrantAsync(
            DailyQuestTaskSaveData task,
            PendingQuestRewardSaveData pending,
            bool reconciledAtStartup)
        {
            int retryIndex = 0;
            while (!_lifetimeCts.IsCancellationRequested)
            {
                await UniTask.Delay(
                    RetryDelays[Math.Min(retryIndex, RetryDelays.Length - 1)],
                    cancellationToken: _lifetimeCts.Token);
                task.rewardQueued = true;
                if (await _repository.StageQuestRewardAsync(
                        _state, pending, _lifetimeCts.Token))
                {
                    _pendingTransactions.Add(pending.transactionId);
                    PublishStateChanged();
                    if (!await TryGrantRewardAsync(
                            pending, reconciledAtStartup, _lifetimeCts.Token))
                        StartRewardRetry(pending, reconciledAtStartup);
                    return;
                }
                task.rewardQueued = false;
                retryIndex++;
            }
        }

        private void OnClockTick(ClockTickPayload payload)
        {
            if (!IsReady || _resetting) return;
            DailyQuestScheduleSO schedule = _catalog.dailySchedule;
            if (schedule.GetDayKey(payload.UtcNow) != _state.dayKey)
                ResetForNewDayAsync().Forget();
            else
                PublishStateChanged();
        }

        private async UniTaskVoid ResetForNewDayAsync()
        {
            _resetting = true;
            try
            {
                var runtimeIds = new List<string>();
                for (int i = 0; i < _state.tasks.Count; i++)
                    runtimeIds.Add(_state.tasks[i].runtimeId);
                _questService.DeactivateQuests(runtimeIds);

                DailyQuestSaveData next = BuildNewDay(
                    _catalog.dailySchedule, _timeProvider.UtcNow);
                if (next == null) return;
                _state = next;
                _pendingTransactions.Clear();
                LoadPendingTransactions();
                await _repository.SaveDailyQuestAsync(
                    _state, true, _lifetimeCts.Token);
                ActivateSavedTasks();
                PublishStateChanged();
            }
            finally
            {
                _resetting = false;
            }
        }

        private DailyQuestSaveData BuildNewDay(
            DailyQuestScheduleSO schedule,
            DateTime utcNow)
        {
            DailyQuestSetSO set = schedule.SelectSet(utcNow);
            if (set == null || set.quests == null || set.quests.Count == 0)
            {
                Debug.LogError("[DailyQuest] Daily schedule has no usable set.");
                return null;
            }

            string dayKey = schedule.GetDayKey(utcNow);
            var result = new DailyQuestSaveData
            {
                dayKey = dayKey,
                setId = set.setId
            };

            for (int i = 0; i < set.quests.Count; i++)
            {
                DailyQuestEntry entry = set.quests[i];
                if (entry?.quest == null) continue;
                string runtimeId =
                    $"daily:{dayKey}:{i}:{entry.quest.questId}";
                var runtime = new QuestRuntimeState(runtimeId, entry.quest);
                result.tasks.Add(new DailyQuestTaskSaveData
                {
                    runtimeId = runtimeId,
                    questDefinitionId = entry.quest.questId,
                    points = entry.points,
                    coinReward = entry.coinReward,
                    quest = runtime.CreateSnapshot()
                });
            }

            if (set.milestones != null)
            {
                for (int i = 0; i < set.milestones.Count; i++)
                {
                    DailyMilestoneDefinition source = set.milestones[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.milestoneId))
                        continue;
                    result.milestones.Add(new DailyMilestoneSaveData
                    {
                        milestoneId = source.milestoneId,
                        requiredPoints = source.requiredPoints,
                        coinReward = source.coinReward
                    });
                }
            }
            return result;
        }

        private void ActivateSavedTasks()
        {
            if (_state?.tasks == null) return;
            for (int i = 0; i < _state.tasks.Count; i++)
            {
                DailyQuestTaskSaveData task = _state.tasks[i];
                if (task == null) continue;
                _questService.ActivateQuest(
                    task.runtimeId, task.questDefinitionId, task.quest);
            }
        }

        private void LoadPendingTransactions()
        {
            IReadOnlyList<PendingQuestRewardSaveData> pending =
                _repository.LoadPendingQuestRewards();
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i] != null)
                    _pendingTransactions.Add(pending[i].transactionId);
            }
        }

        private void ReconcileCompletedTasks()
        {
            if (_state?.tasks == null) return;
            for (int i = 0; i < _state.tasks.Count; i++)
            {
                DailyQuestTaskSaveData task = _state.tasks[i];
                if (task == null || task.rewardQueued) continue;
                QuestRuntimeState runtime = _questService.GetQuestState(task.runtimeId);
                if (runtime?.Status != QuestStatus.Completed) continue;
                task.quest = runtime.CreateSnapshot();
                task.rewardQueued = true;
                QueueTaskRewardAsync(task, true).Forget();
            }
        }

        private async UniTaskVoid ReconcilePendingRewardsAsync()
        {
            IReadOnlyList<PendingQuestRewardSaveData> pending =
                _repository.LoadPendingQuestRewards();
            for (int i = 0; i < pending.Count; i++)
            {
                PendingQuestRewardSaveData reward = pending[i];
                if (reward == null) continue;
                if (!await TryGrantRewardAsync(
                        reward, true, _lifetimeCts.Token))
                    StartRewardRetry(reward, true);
            }
        }

        private void StartRewardRetry(
            PendingQuestRewardSaveData pending,
            bool reconciledAtStartup)
        {
            if (pending == null ||
                !_retryingTransactions.Add(pending.transactionId))
                return;
            RetryRewardAsync(pending, reconciledAtStartup).Forget();
        }

        private async UniTaskVoid RetryRewardAsync(
            PendingQuestRewardSaveData pending,
            bool reconciledAtStartup)
        {
            int retryIndex = 0;
            try
            {
                while (!_lifetimeCts.IsCancellationRequested)
                {
                    await UniTask.Delay(
                        RetryDelays[Math.Min(retryIndex, RetryDelays.Length - 1)],
                        cancellationToken: _lifetimeCts.Token);
                    if (await TryGrantRewardAsync(
                            pending, reconciledAtStartup, _lifetimeCts.Token))
                        return;
                    retryIndex++;
                }
            }
            catch (OperationCanceledException)
            {
                // Normal scene teardown.
            }
            finally
            {
                _retryingTransactions.Remove(pending.transactionId);
            }
        }

        private async UniTask<bool> TryGrantRewardAsync(
            PendingQuestRewardSaveData pending,
            bool reconciledAtStartup,
            CancellationToken cancellationToken)
        {
            QuestRewardGrantResult result =
                await _rewardService.GrantPendingCoinsAsync(
                    pending.transactionId, cancellationToken);
            if (!result.Success) return false;

            _pendingTransactions.Remove(pending.transactionId);
            _rewardPublisher.Publish(new QuestRewardGrantedPayload(
                pending.transactionId,
                pending.kind,
                pending.coins,
                reconciledAtStartup));
            PublishStateChanged();
            return true;
        }

        private int CalculateTotalPoints()
        {
            int total = 0;
            if (_state?.tasks == null) return total;
            for (int i = 0; i < _state.tasks.Count; i++)
            {
                DailyQuestTaskSaveData task = _state.tasks[i];
                QuestRuntimeState runtime =
                    task != null ? _questService.GetQuestState(task.runtimeId) : null;
                if (runtime?.Status == QuestStatus.Completed)
                    total += Math.Max(0, task.points);
            }
            return total;
        }

        private void PublishStateChanged()
        {
            _statePublisher.Publish(new DailyQuestStateChangedPayload(
                _state?.dayKey));
        }

        private static string CreateTaskTransactionId(
            string dayKey, string runtimeId)
        {
            return $"daily-reward:{dayKey}:task:{runtimeId}";
        }

        private static string CreateMilestoneTransactionId(
            string dayKey, string milestoneId)
        {
            return $"daily-reward:{dayKey}:milestone:{milestoneId}";
        }

        private static TimeSpan MaxZero(TimeSpan value)
        {
            return value < TimeSpan.Zero ? TimeSpan.Zero : value;
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }
    }
}
