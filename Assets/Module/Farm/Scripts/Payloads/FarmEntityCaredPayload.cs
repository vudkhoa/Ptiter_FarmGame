using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityCaredPayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly FarmEntityType EntityType;
        public readonly InputRequirement[] InputsApplied;

        public FarmEntityCaredPayload(string entityId, Vector3Int cell, FarmEntityType entityType, InputRequirement[] inputsApplied)
        {
            EntityId = entityId;
            Cell = cell;
            EntityType = entityType;
            InputsApplied = inputsApplied;
        }
    }
}
