using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityStageChangedPayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly FarmEntityType EntityType;
        public readonly int NewStage;

        public FarmEntityStageChangedPayload(string entityId, Vector3Int cell, FarmEntityType entityType, int newStage)
        {
            EntityId = entityId;
            Cell = cell;
            EntityType = entityType;
            NewStage = newStage;
        }
    }
}
