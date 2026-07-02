using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "NewAnimalData", menuName = "GDD/Farm/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Header("Base Configuration")]
        public string animalId;
        public string animalName;
        public int purchaseCost;

        [Header("Production Logic")]
        public float productionTime;

        [Tooltip("Ratio (0.0 to 1.0) of production progress to transition from Stage 1 (Young) to Stage 2 (Growing).")]
        [Range(0f, 1f)]
        public float stage2Threshold = 0.3f;

        public string requiredFoodItemId;

        [Header("Visual Progress")]
        [Tooltip("Should contain exactly 3 sprites representing: Young -> Growing -> Ripe (Ready to produce).")]
        public Sprite[] growthSprites;

        [Header("Yield Configuration")]
        public string yieldItemId;
        public int productAmount;
    }
}
