using System;
using System.Collections.Generic;
using System.Threading;
using Core.Module.Storage;
using Core.Module.Time;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Core.Module.Quest.Cooking
{
    public sealed class CookingService : ICookingService, IDisposable
    {
        private readonly CookingRecipeConfigSO _config;
        private readonly IStorageService _storage;
        private readonly ICookingJobRepository _repository;
        private readonly IServerTimeProvider _timeProvider;
        private readonly IPublisher<CookingStateChangedPayload>
            _statePublisher;
        private readonly IPublisher<CookingCompletedPayload>
            _completedPublisher;
        private readonly IDisposable _inventorySubscription;
        private readonly IDisposable _completionSubscription;

        private CancellationTokenSource _lifetimeCts;
        private CookingJobSaveData _activeJob;
        private int _lastPublishedRemaining = -1;
        private DateTime _nextCompletionRetryUtc = DateTime.MinValue;
        private bool _initialized;

        public CookingService(
            QuestCatalogSO catalog,
            IStorageService storage,
            ICookingJobRepository repository,
            IServerTimeProvider timeProvider,
            IPublisher<CookingStateChangedPayload> statePublisher,
            IPublisher<CookingCompletedPayload> completedPublisher,
            ISubscriber<CookingCompletionCommittedPayload>
                completionSubscriber,
            ISubscriber<InventoryChangedPayload> inventorySubscriber)
        {
            _config = catalog != null ? catalog.cookingRecipeConfig : null;
            _storage = storage;
            _repository = repository;
            _timeProvider = timeProvider;
            _statePublisher = statePublisher;
            _completedPublisher = completedPublisher;
            _completionSubscription = completionSubscriber.Subscribe(
                OnCompletionCommitted);
            _inventorySubscription = inventorySubscriber.Subscribe(
                OnInventoryChanged);
        }

        public void Initialize()
        {
            if (_initialized) return;

            _initialized = true;
            _activeJob = _repository.LoadActiveCookingJob();
            if (!IsValidPersistedJob(_activeJob))
            {
                if (_activeJob != null)
                    _repository.SaveActiveCookingJob(null);
                _activeJob = null;
            }

            _lifetimeCts = new CancellationTokenSource();
            Refresh();
            TimerLoopAsync(_lifetimeCts.Token).Forget();
            PublishStateChanged(_activeJob?.recipeId);
        }

        public CookingRecipeState GetRecipeState(
            string recipeId,
            int requestedQuantity = 1)
        {
            CookingRecipeDefinition recipe = _config?.GetRecipe(recipeId);
            int maxQuantity = Mathf.Max(1, _config?.maxQuantity ?? 99);
            int quantity = Mathf.Clamp(requestedQuantity, 1, maxQuantity);
            if (recipe == null)
            {
                return new CookingRecipeState
                {
                    RecipeId = recipeId,
                    MaxQuantity = maxQuantity,
                    RequestedQuantity = quantity,
                    CanStart = false
                };
            }

            var ingredientStates = new List<CookingIngredientState>();
            int maxCraftable = maxQuantity;
            bool hasIngredient = false;
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    CookingIngredientDefinition ingredient =
                        recipe.ingredients[i];
                    if (ingredient == null ||
                        string.IsNullOrWhiteSpace(ingredient.itemId))
                        continue;

                    hasIngredient = true;
                    int perItem = Mathf.Max(1, ingredient.amountPerItem);
                    int owned = Mathf.Max(
                        0,
                        _storage.GetItemCount(ingredient.itemId));
                    int required = perItem * quantity;
                    maxCraftable = Mathf.Min(
                        maxCraftable,
                        owned / perItem);
                    ingredientStates.Add(new CookingIngredientState(
                        ingredient.itemId,
                        ingredient.displayName,
                        ingredient.icon,
                        perItem,
                        owned,
                        required));
                }
            }

            if (!hasIngredient) maxCraftable = maxQuantity;
            bool hasActiveJob = _activeJob != null;
            bool isThisRecipe = hasActiveJob && string.Equals(
                _activeJob.recipeId,
                recipeId,
                StringComparison.Ordinal);
            int remaining = isThisRecipe
                ? CalculateRemainingSeconds(_activeJob)
                : 0;
            int secondsPerItem = Mathf.Max(1, recipe.secondsPerItem);

            return new CookingRecipeState
            {
                RecipeId = recipe.recipeId,
                DisplayName = recipe.displayName,
                Description = recipe.description,
                DishSprite = recipe.dishSprite,
                SecondsPerItem = secondsPerItem,
                MaxQuantity = maxQuantity,
                RequestedQuantity = quantity,
                MaxCraftable = Mathf.Clamp(
                    maxCraftable,
                    0,
                    maxQuantity),
                IsCooking = isThisRecipe,
                IsBusyWithOtherRecipe = hasActiveJob && !isThisRecipe,
                CompletionPending =
                    isThisRecipe && _activeJob.completionPending,
                CanStart =
                    !hasActiveJob && quantity <= maxCraftable,
                CookingQuantity = isThisRecipe ? _activeJob.quantity : 0,
                RemainingSeconds = remaining,
                TotalSeconds = isThisRecipe
                    ? _activeJob.totalSeconds
                    : secondsPerItem * quantity,
                Ingredients = ingredientStates
            };
        }

        public CookingStartResult TryStartCooking(
            string recipeId,
            int quantity)
        {
            if (!_initialized) Initialize();

            CookingRecipeDefinition recipe = _config?.GetRecipe(recipeId);
            if (recipe == null)
                return Result(CookingStartResultCode.InvalidRecipe, recipeId, quantity);
            if (string.IsNullOrWhiteSpace(recipe.outputItemId) ||
                recipe.outputAmountPerItem <= 0)
                return Result(CookingStartResultCode.InvalidOutput, recipeId, quantity);

            int maxQuantity = Mathf.Max(1, _config?.maxQuantity ?? 99);
            if (quantity < 1 || quantity > maxQuantity)
                return Result(CookingStartResultCode.InvalidQuantity, recipeId, quantity);
            if (_activeJob != null)
                return Result(CookingStartResultCode.Busy, recipeId, quantity);

            CookingRecipeState before = GetRecipeState(recipeId, quantity);
            if (quantity > before.MaxCraftable)
                return new CookingStartResult(
                    CookingStartResultCode.InsufficientIngredients,
                    before);

            var removed = new List<RemovedIngredient>();
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    CookingIngredientDefinition ingredient =
                        recipe.ingredients[i];
                    if (ingredient == null ||
                        string.IsNullOrWhiteSpace(ingredient.itemId))
                        continue;

                    int amount = Mathf.Max(1, ingredient.amountPerItem) *
                                 quantity;
                    if (!_storage.RemoveItem(ingredient.itemId, amount))
                    {
                        RollbackRemovedIngredients(removed);
                        return Result(
                            CookingStartResultCode.ConsumeFailed,
                            recipeId,
                            quantity);
                    }

                    removed.Add(new RemovedIngredient(
                        ingredient.itemId,
                        amount));
                }
            }

            int totalSeconds = Mathf.Max(1, recipe.secondsPerItem) * quantity;
            long outputAmountLong =
                (long)Mathf.Max(1, recipe.outputAmountPerItem) * quantity;
            if (outputAmountLong > int.MaxValue)
            {
                RollbackRemovedIngredients(removed);
                return Result(
                    CookingStartResultCode.InvalidOutput,
                    recipeId,
                    quantity);
            }

            var job = new CookingJobSaveData
            {
                transactionId = $"cooking:{Guid.NewGuid():N}",
                recipeId = recipe.recipeId,
                outputItemId = recipe.outputItemId,
                outputAmount = (int)outputAmountLong,
                quantity = quantity,
                totalSeconds = totalSeconds,
                endsAtUtcTicks = _timeProvider.UtcNow
                    .AddSeconds(totalSeconds)
                    .Ticks,
                completionPending = false
            };

            _activeJob = job;
            if (!_repository.SaveActiveCookingJob(job))
            {
                _activeJob = null;
                RollbackRemovedIngredients(removed);
                return Result(
                    CookingStartResultCode.SaveFailed,
                    recipeId,
                    quantity);
            }

            _lastPublishedRemaining = -1;
            PublishStateChanged(recipeId);
            return new CookingStartResult(
                CookingStartResultCode.Success,
                GetRecipeState(recipeId, quantity));
        }

        public void Refresh()
        {
            if (!_initialized || _activeJob == null) return;

            int remaining = CalculateRemainingSeconds(_activeJob);
            if (remaining <= 0)
            {
                if (!_activeJob.completionPending)
                {
                    CookingJobSaveData pending = Clone(_activeJob);
                    pending.completionPending = true;
                    if (!_repository.SaveActiveCookingJob(pending))
                        return;
                    _activeJob = pending;
                    PublishStateChanged(_activeJob.recipeId);
                }

                PublishCompletionIfDue();
                return;
            }

            if (remaining == _lastPublishedRemaining) return;
            _lastPublishedRemaining = remaining;
            PublishStateChanged(_activeJob.recipeId);
        }

        private void PublishCompletionIfDue()
        {
            if (_activeJob == null) return;

            DateTime now = _timeProvider.UtcNow;
            if (now < _nextCompletionRetryUtc) return;
            _nextCompletionRetryUtc = now.AddSeconds(1);

            _completedPublisher.Publish(new CookingCompletedPayload(
                _activeJob.transactionId,
                _activeJob.recipeId,
                _activeJob.outputItemId,
                _activeJob.outputAmount,
                _activeJob.quantity));
        }

        private void OnCompletionCommitted(
            CookingCompletionCommittedPayload payload)
        {
            if (_activeJob == null || !string.Equals(
                    _activeJob.transactionId,
                    payload.TransactionId,
                    StringComparison.Ordinal))
                return;

            string recipeId = _activeJob.recipeId;
            _activeJob = null;
            _lastPublishedRemaining = -1;
            _nextCompletionRetryUtc = DateTime.MinValue;
            PublishStateChanged(recipeId);
        }

        private void OnInventoryChanged(InventoryChangedPayload _)
        {
            if (_initialized)
                PublishStateChanged(string.Empty);
        }

        private async UniTaskVoid TimerLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(
                        TimeSpan.FromMilliseconds(250),
                        ignoreTimeScale: true,
                        cancellationToken: token);
                    Refresh();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal Game scope teardown.
            }
        }

        private int CalculateRemainingSeconds(CookingJobSaveData job)
        {
            if (job == null || job.endsAtUtcTicks <= 0) return 0;

            DateTime endsAt = new DateTime(
                job.endsAtUtcTicks,
                DateTimeKind.Utc);
            double remaining = (endsAt - _timeProvider.UtcNow).TotalSeconds;
            return Mathf.Max(0, Mathf.CeilToInt((float)remaining));
        }

        private bool IsValidPersistedJob(CookingJobSaveData job)
        {
            if (job == null) return true;
            return !string.IsNullOrWhiteSpace(job.transactionId) &&
                   !string.IsNullOrWhiteSpace(job.recipeId) &&
                   !string.IsNullOrWhiteSpace(job.outputItemId) &&
                   job.quantity > 0 &&
                   job.outputAmount > 0 &&
                   job.totalSeconds > 0 &&
                   job.endsAtUtcTicks > 0 &&
                   _config?.GetRecipe(job.recipeId) != null;
        }

        private CookingStartResult Result(
            CookingStartResultCode code,
            string recipeId,
            int quantity)
        {
            return new CookingStartResult(
                code,
                GetRecipeState(recipeId, quantity));
        }

        private void RollbackRemovedIngredients(
            List<RemovedIngredient> removed)
        {
            for (int i = removed.Count - 1; i >= 0; i--)
                _storage.AddItem(removed[i].ItemId, removed[i].Amount);
        }

        private void PublishStateChanged(string recipeId)
        {
            _statePublisher.Publish(
                new CookingStateChangedPayload(recipeId ?? string.Empty));
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

        public void Dispose()
        {
            _inventorySubscription?.Dispose();
            _completionSubscription?.Dispose();
            if (_lifetimeCts == null) return;

            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _lifetimeCts = null;
        }

        private readonly struct RemovedIngredient
        {
            public string ItemId { get; }
            public int Amount { get; }

            public RemovedIngredient(string itemId, int amount)
            {
                ItemId = itemId;
                Amount = amount;
            }
        }
    }
}
