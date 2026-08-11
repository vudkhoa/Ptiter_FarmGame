using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest.Cooking
{
    public sealed class CookingIngredientState
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public int AmountPerItem { get; }
        public int OwnedAmount { get; }
        public int RequiredAmount { get; }
        public bool HasEnough => OwnedAmount >= RequiredAmount;

        public CookingIngredientState(
            string itemId,
            string displayName,
            Sprite icon,
            int amountPerItem,
            int ownedAmount,
            int requiredAmount)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Icon = icon;
            AmountPerItem = amountPerItem;
            OwnedAmount = ownedAmount;
            RequiredAmount = requiredAmount;
        }
    }

    public sealed class CookingRecipeState
    {
        public string RecipeId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Sprite DishSprite { get; set; }
        public int SecondsPerItem { get; set; }
        public int MaxQuantity { get; set; }
        public int RequestedQuantity { get; set; }
        public int MaxCraftable { get; set; }
        public bool IsCooking { get; set; }
        public bool IsBusyWithOtherRecipe { get; set; }
        public bool CompletionPending { get; set; }
        public bool CanStart { get; set; }
        public int CookingQuantity { get; set; }
        public int RemainingSeconds { get; set; }
        public int TotalSeconds { get; set; }
        public IReadOnlyList<CookingIngredientState> Ingredients { get; set; }
            = new List<CookingIngredientState>();
    }

    public enum CookingStartResultCode
    {
        Success = 0,
        InvalidRecipe = 1,
        InvalidOutput = 2,
        InvalidQuantity = 3,
        Busy = 4,
        InsufficientIngredients = 5,
        ConsumeFailed = 6,
        SaveFailed = 7
    }

    public readonly struct CookingStartResult
    {
        public CookingStartResultCode Code { get; }
        public CookingRecipeState State { get; }
        public bool IsSuccess => Code == CookingStartResultCode.Success;

        public CookingStartResult(
            CookingStartResultCode code,
            CookingRecipeState state)
        {
            Code = code;
            State = state;
        }
    }
}
