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

        [Tooltip("Tỉ lệ thời gian bắt đầu chuyển sang Giai đoạn 2 (Đang phát triển)")]
        [Range(0f, 1f)]
        public float stage2Threshold = 0.3f;

        [Header("Visual Progress")]
        [Tooltip("Phải có đủ 3 sprites tương ứng với 3 giai đoạn")]
        public Sprite[] growthSprites;

        [Header("Harvest Reward")]
        public string yieldItemId;
        public int harvestAmount;
        public int maxHarvestBatches;
    }
}
