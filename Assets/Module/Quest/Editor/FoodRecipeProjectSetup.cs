#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.Module.Quest.Editor
{
    public static class FoodRecipeProjectSetup
    {
        private const string ConfigFolder =
            "Assets/Module/Quest/Configs/Food";
        private const string ConfigPath =
            ConfigFolder + "/FoodRecipeConfig.asset";
        private const string CatalogPath =
            "Assets/Module/Quest/Configs/QuestCatalog.asset";
        private const string ProgressPath =
            "Assets/Module/Quest/Configs/Progress/ProgressQuestConfig.asset";
        private const string TextureRoot =
            "Assets/Module/Quest/Texture/";

        [MenuItem("Tools/Quest/Ensure Food Recipe Logic")]
        public static void EnsureFromMenu()
        {
            Ensure();
            AssetDatabase.SaveAssets();
            Debug.Log("[QuestSetup] Food recipe logic and mock assets are ready.");
        }

        public static void Ensure()
        {
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                if (!AssetDatabase.IsValidFolder(
                        "Assets/Module/Quest/Configs"))
                    AssetDatabase.CreateFolder(
                        "Assets/Module/Quest", "Configs");
                AssetDatabase.CreateFolder(
                    "Assets/Module/Quest/Configs", "Food");
            }

            FoodRecipeConfigSO config =
                AssetDatabase.LoadAssetAtPath<FoodRecipeConfigSO>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FoodRecipeConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.lockIcon = LoadSprite("quest khóa 1.png");
            config.recipes = new List<FoodRecipeDefinition>
            {
                new FoodRecipeDefinition
                {
                    recipeId = "banh_mi_heo_quay",
                    displayName = "BÁNH MÌ HEO QUAY",
                    mockIngredients = "Nguyên liệu đang được cập nhật",
                    starCost = 30,
                    prerequisiteRecipeId = string.Empty,
                    mockSprite = LoadSprite("Thịt kho tàu 1.png")
                },
                new FoodRecipeDefinition
                {
                    recipeId = "nem_ran",
                    displayName = "NEM RÁN",
                    mockIngredients = "Nguyên liệu đang được cập nhật",
                    starCost = 50,
                    prerequisiteRecipeId = "banh_mi_heo_quay",
                    mockSprite = LoadSprite("Nem_rán 1.png")
                }
            };
            EditorUtility.SetDirty(config);

            QuestCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<QuestCatalogSO>(CatalogPath);
            if (catalog != null)
            {
                catalog.foodRecipeConfig = config;
                EditorUtility.SetDirty(catalog);
            }

            ProgressQuestConfigSO progress =
                AssetDatabase.LoadAssetAtPath<ProgressQuestConfigSO>(
                    ProgressPath);
            if (progress?.milestones != null &&
                progress.milestones.Count >= 3)
            {
                progress.milestones[0].starReward = 10;
                progress.milestones[1].starReward = 20;
                progress.milestones[2].starReward = 50;
                EditorUtility.SetDirty(progress);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(TextureRoot + fileName);
        }
    }
}
#endif
