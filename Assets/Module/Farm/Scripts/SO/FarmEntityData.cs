using UnityEngine;

namespace Core.Module.Farm
{
    public enum FarmEntityType
    {
        Crop,
        Animal
    }

    [System.Serializable]
    public struct InputRequirement
    {
        public ItemDataSO item;
        public int amount;
    }

    [System.Serializable]
    public struct OutputReward
    {
        public ItemDataSO item;
        public int amount;
    }

    [CreateAssetMenu(fileName = "NewFarmEntity", menuName = "GDD/Farm/Farm Entity Data")]
    public class FarmEntityData : ScriptableObject
    {
        public string EntityId => name;
        public string entityName;
        public FarmEntityType entityType;

        [Header("Purchase Config")]
        public int coinCost;

        [Header("Requirements & Yields")]
        public InputRequirement[] inputs;
        public OutputReward[] outputs;

        [Header("Growth Config")]
        public float processTime;
        
        [Range(0f, 1f)]
        public float stage2Threshold = 0.3f;

        [Tooltip("Chứa đúng 3 sprites: Giai đoạn mầm -> Đang lớn -> Thu hoạch")]
        public Sprite[] growthSprites;

        [Header("Lifecycle Control")]
        public bool autoRestart;
        public int maxCycles = 1;
    }
}
