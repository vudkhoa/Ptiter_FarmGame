using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Quest
{
    public sealed class ProgressQuestService :
        IProgressQuestService,
        IStarWalletService,
        IDisposable
    {
        private readonly QuestCatalogSO _catalog;
        private readonly IProgressQuestRepository _repository;
        private readonly IPublisher<ProgressQuestStateChangedPayload> _statePublisher;
        private readonly IPublisher<ProgressRewardClaimedPayload> _rewardPublisher;
        private readonly IDisposable _coinsEarnedSubscription;
        private readonly Queue<ProgressCoinsEarnedPayload> _pendingCredits =
            new Queue<ProgressCoinsEarnedPayload>();
        private readonly HashSet<string> _claimingMilestones =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _purchasingUnlocks =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly CancellationTokenSource _lifetimeCts =
            new CancellationTokenSource();

        private ProgressQuestSaveData _state;
        private bool _initializing;
        private bool _processingCredits;

        public bool IsReady { get; private set; }
        public int Stars => _state?.stars ?? 0;

        public ProgressQuestService(
            QuestCatalogSO catalog,
            IProgressQuestRepository repository,
            ISubscriber<ProgressCoinsEarnedPayload> coinsEarnedSubscriber,
            IPublisher<ProgressQuestStateChangedPayload> statePublisher,
            IPublisher<ProgressRewardClaimedPayload> rewardPublisher)
        {
            _catalog = catalog;
            _repository = repository;
            _statePublisher = statePublisher;
            _rewardPublisher = rewardPublisher;
            _coinsEarnedSubscription =
                coinsEarnedSubscriber.Subscribe(OnCoinsEarned);
        }

        public async UniTask EnsureInitializedAsync(
            CancellationToken cancellationToken = default)
        {
            if (IsReady) return;
            if (_initializing)
            {
                await UniTask.WaitUntil(
                    () => !_initializing || IsReady,
                    cancellationToken: cancellationToken);
                return;
            }
            _initializing = true;
            try
            {
                await _repository.WaitUntilLoadedAsync(cancellationToken);
                if (_catalog == null || _catalog.progressConfig == null)
                {
                    Debug.LogError(
                        "[ProgressQuest] QuestCatalogSO has no ProgressQuestConfigSO.");
                    return;
                }

                _state = _repository.LoadProgressQuest() ??
                         new ProgressQuestSaveData();
                NormalizeState(_state);
                IsReady = true;

                bool persisted = await _repository.SaveProgressQuestAsync(
                    _state, cancellationToken);
                if (!persisted)
                {
                    IsReady = false;
                    Debug.LogError("[ProgressQuest] Could not persist initial state.");
                    return;
                }

                PublishStateChanged();
                ProcessPendingCreditsAsync().Forget();
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

        public ProgressQuestViewState GetViewState()
        {
            ProgressQuestConfigSO config =
                _catalog != null ? _catalog.progressConfig : null;
            if (!IsReady || _state == null || config == null)
            {
                return new ProgressQuestViewState
                {
                    IsReady = false,
                    LockedReason = "Progress is loading...",
                    Milestones = Array.Empty<ProgressMilestoneViewData>()
                };
            }

            var views = new List<ProgressMilestoneViewData>();
            if (config.milestones != null)
            {
                var usedIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < config.milestones.Count; i++)
                {
                    ProgressMilestoneDefinition milestone = config.milestones[i];
                    if (milestone == null ||
                        string.IsNullOrWhiteSpace(milestone.milestoneId) ||
                        !usedIds.Add(milestone.milestoneId))
                        continue;

                    bool claimed = _state.claimedMilestoneIds.Contains(
                        milestone.milestoneId);
                    ProgressMilestoneClaimState claimState = claimed
                        ? ProgressMilestoneClaimState.Claimed
                        : _state.accumulatedCoins >= milestone.requiredCoins
                            ? ProgressMilestoneClaimState.Claimable
                            : ProgressMilestoneClaimState.Locked;
                    views.Add(new ProgressMilestoneViewData
                    {
                        MilestoneId = milestone.milestoneId,
                        CurrentCoins = Math.Min(
                            _state.accumulatedCoins,
                            Math.Max(1, milestone.requiredCoins)),
                        RequiredCoins = Math.Max(1, milestone.requiredCoins),
                        StarReward = Math.Max(1, milestone.starReward),
                        ClaimState = claimState
                    });
                }
            }

            return new ProgressQuestViewState
            {
                IsReady = true,
                AccumulatedCoins = _state.accumulatedCoins,
                Stars = _state.stars,
                Milestones = views
            };
        }

        public async UniTask<bool> ClaimMilestoneAsync(
            string milestoneId,
            CancellationToken cancellationToken = default)
        {
            if (!IsReady || string.IsNullOrWhiteSpace(milestoneId) ||
                !_claimingMilestones.Add(milestoneId))
                return false;

            try
            {
                ProgressMilestoneDefinition milestone = FindMilestone(milestoneId);
                if (milestone == null ||
                    _state.claimedMilestoneIds.Contains(milestoneId) ||
                    _state.accumulatedCoins < milestone.requiredCoins)
                    return false;

                int previousStars = _state.stars;
                _state.claimedMilestoneIds.Add(milestoneId);
                _state.stars = AddClamped(
                    _state.stars, Math.Max(1, milestone.starReward));

                if (!await _repository.SaveProgressQuestAsync(
                        _state, cancellationToken))
                {
                    _state.claimedMilestoneIds.Remove(milestoneId);
                    _state.stars = previousStars;
                    return false;
                }

                _rewardPublisher.Publish(new ProgressRewardClaimedPayload(
                    milestoneId, Math.Max(1, milestone.starReward)));
                PublishStateChanged();
                return true;
            }
            finally
            {
                _claimingMilestones.Remove(milestoneId);
            }
        }

        public bool IsStarUnlockPurchased(string unlockId)
        {
            return IsReady &&
                   !string.IsNullOrWhiteSpace(unlockId) &&
                   _state?.unlockedRecipeIds != null &&
                   _state.unlockedRecipeIds.Contains(unlockId);
        }

        public async UniTask<StarUnlockPurchaseResult>
            TryPurchaseStarUnlockAsync(
                string unlockId,
                int cost,
                CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            if (!IsReady || string.IsNullOrWhiteSpace(unlockId) || cost <= 0)
                return new StarUnlockPurchaseResult(
                    StarUnlockPurchaseState.InvalidRequest,
                    Stars);
            if (IsStarUnlockPurchased(unlockId))
                return new StarUnlockPurchaseResult(
                    StarUnlockPurchaseState.AlreadyUnlocked,
                    Stars);
            if (!_purchasingUnlocks.Add(unlockId))
                return new StarUnlockPurchaseResult(
                    StarUnlockPurchaseState.Busy,
                    Stars);

            try
            {
                if (_state.stars < cost)
                    return new StarUnlockPurchaseResult(
                        StarUnlockPurchaseState.InsufficientStars,
                        _state.stars);

                int previousStars = _state.stars;
                _state.stars -= cost;
                _state.unlockedRecipeIds.Add(unlockId);
                if (!await _repository.SaveProgressQuestAsync(
                        _state,
                        cancellationToken))
                {
                    _state.unlockedRecipeIds.Remove(unlockId);
                    _state.stars = previousStars;
                    return new StarUnlockPurchaseResult(
                        StarUnlockPurchaseState.SaveFailed,
                        _state.stars);
                }

                PublishStateChanged();
                return new StarUnlockPurchaseResult(
                    StarUnlockPurchaseState.Success,
                    _state.stars);
            }
            finally
            {
                _purchasingUnlocks.Remove(unlockId);
            }
        }

        private void OnCoinsEarned(ProgressCoinsEarnedPayload payload)
        {
            if (payload.Amount <= 0) return;
            _pendingCredits.Enqueue(payload);
            if (!IsReady)
                EnsureInitializedAsync(_lifetimeCts.Token).Forget();
            else
                ProcessPendingCreditsAsync().Forget();
        }

        private async UniTaskVoid ProcessPendingCreditsAsync()
        {
            if (_processingCredits || !IsReady) return;
            _processingCredits = true;
            try
            {
                while (_pendingCredits.Count > 0 &&
                       !_lifetimeCts.IsCancellationRequested)
                {
                    ProgressCoinsEarnedPayload payload = _pendingCredits.Dequeue();
                    int previousCoins = _state.accumulatedCoins;
                    _state.accumulatedCoins = AddClamped(
                        _state.accumulatedCoins, payload.Amount);
                    if (!await _repository.SaveProgressQuestAsync(
                            _state, _lifetimeCts.Token))
                    {
                        _state.accumulatedCoins = previousCoins;
                        Debug.LogError(
                            $"[ProgressQuest] Failed to persist credit {payload.TransactionId}.");
                        continue;
                    }

                    PublishStateChanged();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal scene teardown.
            }
            finally
            {
                _processingCredits = false;
            }
        }

        private ProgressMilestoneDefinition FindMilestone(string milestoneId)
        {
            List<ProgressMilestoneDefinition> milestones =
                _catalog?.progressConfig?.milestones;
            if (milestones == null) return null;
            return milestones.Find(item => item != null &&
                string.Equals(item.milestoneId, milestoneId,
                    StringComparison.Ordinal));
        }

        private void PublishStateChanged()
        {
            _statePublisher.Publish(new ProgressQuestStateChangedPayload(
                _state?.accumulatedCoins ?? 0,
                _state?.stars ?? 0));
        }

        private static void NormalizeState(ProgressQuestSaveData state)
        {
            state.accumulatedCoins = Math.Max(0, state.accumulatedCoins);
            state.stars = Math.Max(0, state.stars);
            state.claimedMilestoneIds ??= new List<string>();
            state.unlockedRecipeIds ??= new List<string>();
            var usedUnlockIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = state.unlockedRecipeIds.Count - 1; i >= 0; i--)
            {
                string unlockId = state.unlockedRecipeIds[i];
                if (string.IsNullOrWhiteSpace(unlockId) ||
                    !usedUnlockIds.Add(unlockId))
                    state.unlockedRecipeIds.RemoveAt(i);
            }
        }

        private static int AddClamped(int current, int amount)
        {
            long result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        public void Dispose()
        {
            _coinsEarnedSubscription?.Dispose();
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _pendingCredits.Clear();
            _claimingMilestones.Clear();
            _purchasingUnlocks.Clear();
        }
    }
}
