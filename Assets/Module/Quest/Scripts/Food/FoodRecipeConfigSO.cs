using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class FoodRecipeDefinition
    {
        public string recipeId;
        public string displayName;
        [TextArea] public string mockIngredients;
        [Min(1)] public int starCost = 1;
        public string prerequisiteRecipeId;
        public Sprite mockSprite;
    }

    [CreateAssetMenu(
        fileName = "FoodRecipeConfig",
        menuName = "GDD/Quest/Food Recipe Config")]
    public sealed class FoodRecipeConfigSO : ScriptableObject
    {
        public Sprite lockIcon;
        public List<FoodRecipeDefinition> recipes =
            new List<FoodRecipeDefinition>();

        public FoodRecipeDefinition GetRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || recipes == null)
                return null;

            for (int i = 0; i < recipes.Count; i++)
            {
                FoodRecipeDefinition recipe = recipes[i];
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
            if (recipes == null) return;
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++)
            {
                FoodRecipeDefinition recipe = recipes[i];
                if (recipe == null) continue;
                recipe.starCost = Mathf.Max(1, recipe.starCost);
                if (string.IsNullOrWhiteSpace(recipe.recipeId))
                {
                    Debug.LogWarning(
                        $"[FoodRecipe] Recipe at index {i} has no ID.", this);
                    continue;
                }
                if (!usedIds.Add(recipe.recipeId))
                    Debug.LogWarning(
                        $"[FoodRecipe] Duplicate ID '{recipe.recipeId}'.", this);
            }
        }
    }
}
