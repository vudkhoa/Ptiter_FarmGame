using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Farm
{
    public interface IFarmService
    {
        void Initialize(List<FarmSlotSaveData> savedSlots, long lastSaveUtcTicks);
        bool TryPlant(Vector3Int cell, string entityId);
        bool TryFeed(Vector3Int cell);
        bool TryHarvest(Vector3Int cell, out string productItemId, out int amount);
        FarmSlotSaveData GetSlotAt(Vector3Int cell);
        IReadOnlyList<FarmSlotSaveData> ActiveSlots { get; }
    }
}
