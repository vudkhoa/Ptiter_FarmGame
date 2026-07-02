using System;

namespace Core.Module.Farm
{
    [Serializable]
    public class FarmSlotSaveData
    {
        public int cellX;
        public int cellY;
        public int cellZ;

        public bool isAnimal;            // true = Animal/Barn, false = Crop/Field
        public string entityId;          // Crop ID or Animal ID from SO config
        public FarmSlotState state;

        public float growthTimeSec;      // Accumulated growth time (seconds)
        public long startTimeUtcTicks;   // Authoritative UTC start time
        public long lastUpdateUtcTicks;  // Authoritative UTC last update time
        public int remainingHarvests;    // Sugarcane remaining harvests
        public bool isFed;               // Feed status for animals
    }
}
