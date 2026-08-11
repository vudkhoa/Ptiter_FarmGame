using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest.Cooking
{
    [Serializable]
    public sealed class CookingIngredientDefinition
    {
        public string itemId;
        public string displayName;
        [Min(1)] public int amountPerItem = 1;
        public Sprite icon;
    }

    [Serializable]
    public sealed class CookingRecipeDefinition
    {
        public string recipeId;
        public string displayName;
        [TextArea] public string description;
        public Sprite dishSprite;
        [Min(1)] public int secondsPerItem = 20;
        public string outputItemId;
        [Min(1)] public int outputAmountPerItem = 1;
        public List<CookingIngredientDefinition> ingredients =
            new List<CookingIngredientDefinition>();
    }

    [CreateAssetMenu(
        fileName = "CookingRecipeConfig",
        menuName = "GDD/Quest/Cooking Recipe Config")]
    public sealed class CookingRecipeConfigSO : ScriptableObject
    {
        [Min(1)] public int maxQuantity = 99;
        public List<CookingRecipeDefinition> recipes =
            new List<CookingRecipeDefinition>();

        public CookingRecipeDefinition GetRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || recipes == null)
                return null;

            for (int i = 0; i < recipes.Count; i++)
            {
                CookingRecipeDefinition recipe = recipes[i];
                if (recipe != null && string.Equals(
                        recipe.recipeId,
                        recipeId,
                        StringComparison.Ordinal))
                    return recipe;
            }

            return null;
        }

        private void OnValidate()
        {
            maxQuantity = Mathf.Max(1, maxQuantity);
            if (recipes == null) return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++)
            {
                CookingRecipeDefinition recipe = recipes[i];
                if (recipe == null) continue;

                recipe.secondsPerItem = Mathf.Max(1, recipe.secondsPerItem);
                recipe.outputAmountPerItem =
                    Mathf.Max(1, recipe.outputAmountPerItem);
                if (!string.IsNullOrWhiteSpace(recipe.recipeId) &&
                    !ids.Add(recipe.recipeId))
                {
                    Debug.LogWarning(
                        $"[Cooking] Duplicate recipe ID '{recipe.recipeId}'.",
                        this);
                }

                if (recipe.ingredients == null) continue;
                for (int ingredientIndex = 0;
                     ingredientIndex < recipe.ingredients.Count;
                     ingredientIndex++)
                {
                    CookingIngredientDefinition ingredient =
                        recipe.ingredients[ingredientIndex];
                    if (ingredient != null)
                    {
                        ingredient.amountPerItem =
                            Mathf.Max(1, ingredient.amountPerItem);
                    }
                }
            }
        }
    }
}
