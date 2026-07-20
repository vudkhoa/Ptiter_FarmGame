using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityRipePayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly FarmEntityType EntityType;

        public FarmEntityRipePayload(string entityId, Vector3Int cell, FarmEntityType entityType)
        {
            EntityId = entityId;
            Cell = cell;
            EntityType = entityType;
        }
    }
}
