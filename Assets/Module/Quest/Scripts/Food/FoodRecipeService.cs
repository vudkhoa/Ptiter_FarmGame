using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace Core.Module.Quest
{
    public sealed class FoodRecipeService : IFoodRecipeService
    {
        private readonly FoodRecipeConfigSO _config;
        private readonly IStarWalletService _starWallet;
        private readonly IPublisher<FoodRecipeStateChangedPayload>
            _statePublisher;
        private readonly IPublisher<FoodRecipeUnlockedPayload>
            _unlockedPublisher;

        public bool IsReady => _starWallet.IsReady && _config != null;

        public FoodRecipeService(
            QuestCatalogSO catalog,
            IStarWalletService starWallet,
            IPublisher<FoodRecipeStateChangedPayload> statePublisher,
            IPublisher<FoodRecipeUnlockedPayload> unlockedPublisher)
        {
            _config = catalog != null ? catalog.foodRecipeConfig : null;
            _starWallet = starWallet;
            _statePublisher = statePublisher;
            _unlockedPublisher = unlockedPublisher;
        }

        public UniTask EnsureInitializedAsync(
            CancellationToken cancellationToken = default)
        {
            return _starWallet.EnsureInitializedAsync(cancellationToken);
        }

        public FoodRecipeViewState GetViewState()
        {
            if (!IsReady || _config.recipes == null)
            {
                return new FoodRecipeViewState
                {
                    IsReady = false,
                    Recipes = Array.Empty<FoodRecipeViewData>()
                };
            }

            var recipes = new List<FoodRecipeViewData>();
            for (int i = 0; i < _config.recipes.Count; i++)
            {
                FoodRecipeDefinition definition = _config.recipes[i];
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.recipeId))
                    continue;

                bool unlocked = _starWallet.IsStarUnlockPurchased(
                    definition.recipeId);
                bool hasCutscene = definition.HasPlayableCutscene;
                bool prerequisiteMet =
                    string.IsNullOrWhiteSpace(definition.prerequisiteRecipeId) ||
                    _starWallet.IsStarUnlockPurchased(
                        definition.prerequisiteRecipeId);
                FoodRecipeAccessState access = !hasCutscene
                    ? FoodRecipeAccessState.InDevelopment
                    : unlocked
                        ? FoodRecipeAccessState.Unlocked
                        : prerequisiteMet
                            ? FoodRecipeAccessState.Locked
                            : FoodRecipeAccessState.PrerequisiteLocked;

                recipes.Add(new FoodRecipeViewData
                {
                    RecipeId = definition.recipeId,
                    DisplayName = unlocked ? definition.displayName : string.Empty,
                    MockIngredients = unlocked
                        ? definition.mockIngredients
                        : string.Empty,
                    StarCost = Math.Max(1, definition.starCost),
                    MockSprite = definition.mockSprite,
                    CutsceneId = hasCutscene
                        ? definition.cutscene.cutsceneId
                        : string.Empty,
                    AccessState = access
                });
            }

            return new FoodRecipeViewState
            {
                IsReady = true,
                Stars = _starWallet.Stars,
                LockIcon = _config.lockIcon,
                Recipes = recipes
            };
        }

        public async UniTask<FoodRecipeUnlockResult> TryUnlockAsync(
            string recipeId,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            FoodRecipeDefinition definition = _config?.GetRecipe(recipeId);
            if (definition == null)
                return Result(
                    FoodRecipeUnlockResultCode.InvalidRecipe, recipeId, 0);

            int cost = Math.Max(1, definition.starCost);
            string cutsceneId = definition.HasPlayableCutscene
                ? definition.cutscene.cutsceneId
                : string.Empty;
            if (string.IsNullOrWhiteSpace(cutsceneId))
            {
                return Result(
                    FoodRecipeUnlockResultCode.InDevelopment,
                    recipeId,
                    0,
                    string.Empty);
            }

            if (_starWallet.IsStarUnlockPurchased(recipeId))
                return Result(
                    FoodRecipeUnlockResultCode.AlreadyUnlocked,
                    recipeId,
                    cost,
                    cutsceneId);

            if (!string.IsNullOrWhiteSpace(definition.prerequisiteRecipeId) &&
                !_starWallet.IsStarUnlockPurchased(
                    definition.prerequisiteRecipeId))
                return Result(
                    FoodRecipeUnlockResultCode.PrerequisiteLocked,
                    recipeId,
                    cost,
                    cutsceneId);

            StarUnlockPurchaseResult purchase =
                await _starWallet.TryPurchaseStarUnlockAsync(
                    recipeId,
                    cost,
                    cancellationToken);
            FoodRecipeUnlockResultCode code = purchase.State switch
            {
                StarUnlockPurchaseState.Success =>
                    FoodRecipeUnlockResultCode.Success,
                StarUnlockPurchaseState.AlreadyUnlocked =>
                    FoodRecipeUnlockResultCode.AlreadyUnlocked,
                StarUnlockPurchaseState.InsufficientStars =>
                    FoodRecipeUnlockResultCode.InsufficientStars,
                StarUnlockPurchaseState.Busy =>
                    FoodRecipeUnlockResultCode.Busy,
                _ => FoodRecipeUnlockResultCode.SaveFailed
            };

            if (code == FoodRecipeUnlockResultCode.Success)
            {
                _statePublisher.Publish(
                    new FoodRecipeStateChangedPayload(recipeId));
                _unlockedPublisher.Publish(
                    new FoodRecipeUnlockedPayload(recipeId, cost));
            }
            return Result(code, recipeId, cost, cutsceneId);
        }

        private static FoodRecipeUnlockResult Result(
            FoodRecipeUnlockResultCode code,
            string recipeId,
            int requiredStars,
            string cutsceneId = "")
        {
            return new FoodRecipeUnlockResult(
                code,
                recipeId,
                requiredStars,
                cutsceneId);
        }
    }
}
