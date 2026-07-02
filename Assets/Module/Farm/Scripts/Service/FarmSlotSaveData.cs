using UnityEngine;

namespace Core.Module.Farm
{
    [System.Serializable]
    public class FarmSlotSaveData
    {
        public int cellX;
        public int cellY;
        public int cellZ;

        public bool isAnimal;
        public string entityId;
        public FarmSlotState state;

        public float growthTimeSec;
        public long startTimeUtcTicks;
        public long lastUpdateUtcTicks;
        public int remainingHarvests;
        public bool isFed;
    }
}
