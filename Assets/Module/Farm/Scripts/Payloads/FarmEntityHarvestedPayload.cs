using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityHarvestedPayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly FarmEntityType EntityType;
        public readonly OutputReward[] Outputs;

        public FarmEntityHarvestedPayload(
            string entityId,
            Vector3Int cell,
            FarmEntityType entityType,
            OutputReward[] outputs)
        {
            EntityId = entityId;
            Cell = cell;
            EntityType = entityType;
            Outputs = outputs;
        }
    }
}
