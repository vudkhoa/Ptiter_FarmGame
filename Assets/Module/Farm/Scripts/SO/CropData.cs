using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "NewCropData", menuName = "GDD/Farm/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("Base Configuration")]
        public string cropId;
        public string cropName;
        public int coinCost;

        [Header("Growth Logic")]
        public float growTime;

        [Tooltip("Ratio (0.0 to 1.0) of growth progress to transition from Stage 1 (Seed) to Stage 2 (Growing).")]
        [Range(0f, 1f)]
        public float stage2Threshold = 0.3f;

        [Header("Visual Progress")]
        [Tooltip("Should contain exactly 3 sprites representing: Seed/Young -> Growing -> Ripe.")]
        public Sprite[] growthSprites;

        [Header("Harvest Reward")]
        public string yieldItemId;
        public int harvestAmount;
        public int maxHarvestBatches = 1;
    }
}
