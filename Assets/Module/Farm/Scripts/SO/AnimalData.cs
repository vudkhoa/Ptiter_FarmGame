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

        [Tooltip("Tỉ lệ thời gian bắt đầu chuyển sang Giai đoạn 2 (Đang phát triển)")]
        [Range(0f, 1f)]
        public float stage2Threshold = 0.3f;

        public string requiredFoodItemId;

        [Header("Visual Progress")]
        [Tooltip("Phải có đủ 3 sprites tương ứng với 3 giai đoạn")]
        public Sprite[] growthSprites;

        [Header("Yield Configuration")]
        public string yieldItemId;
        public int productAmount;
    }
}
