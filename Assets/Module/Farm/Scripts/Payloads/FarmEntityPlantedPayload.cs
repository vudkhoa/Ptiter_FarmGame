using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityPlantedPayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly FarmEntityType EntityType;

        public FarmEntityPlantedPayload(string entityId, Vector3Int cell, FarmEntityType entityType)
        {
            EntityId = entityId;
            Cell = cell;
            EntityType = entityType;
        }
    }
}
