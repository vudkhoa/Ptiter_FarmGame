using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    public enum FoodRecipeAccessState
    {
        Locked = 0,
        PrerequisiteLocked = 1,
        Unlocked = 2,
        InDevelopment = 3
    }

    public sealed class FoodRecipeViewData
    {
        public string RecipeId { get; internal set; }
        public string DisplayName { get; internal set; }
        public string MockIngredients { get; internal set; }
        public string Story { get; internal set; }
        public int StarCost { get; internal set; }
        public Sprite MockSprite { get; internal set; }
        public string CutsceneId { get; internal set; }
        public FoodRecipeAccessState AccessState { get; internal set; }
    }

    public sealed class FoodRecipeViewState
    {
        public bool IsReady { get; internal set; }
        public int Stars { get; internal set; }
        public Sprite LockIcon { get; internal set; }
        public Sprite CookButtonSprite { get; internal set; }
        public IReadOnlyList<FoodRecipeViewData> Recipes { get; internal set; }
    }

    public enum FoodRecipeUnlockResultCode
    {
        Success = 0,
        AlreadyUnlocked = 1,
        InsufficientStars = 2,
        PrerequisiteLocked = 3,
        InvalidRecipe = 4,
        SaveFailed = 5,
        Busy = 6,
        InDevelopment = 7
    }

    public readonly struct FoodRecipeUnlockResult
    {
        public readonly FoodRecipeUnlockResultCode Code;
        public readonly string RecipeId;
        public readonly int RequiredStars;
        public readonly string CutsceneId;

        public bool Succeeded =>
            Code == FoodRecipeUnlockResultCode.Success ||
            Code == FoodRecipeUnlockResultCode.AlreadyUnlocked;

        public FoodRecipeUnlockResult(
            FoodRecipeUnlockResultCode code,
            string recipeId,
            int requiredStars,
            string cutsceneId)
        {
            Code = code;
            RecipeId = recipeId;
            RequiredStars = requiredStars;
            CutsceneId = cutsceneId;
        }
    }

    public readonly struct FoodRecipeStateChangedPayload
    {
        public readonly string RecipeId;

        public FoodRecipeStateChangedPayload(string recipeId)
        {
            RecipeId = recipeId;
        }
    }

    public readonly struct FoodRecipeUnlockedPayload
    {
        public readonly string RecipeId;
        public readonly int StarsSpent;

        public FoodRecipeUnlockedPayload(string recipeId, int starsSpent)
        {
            RecipeId = recipeId;
            StarsSpent = starsSpent;
        }
    }
}
