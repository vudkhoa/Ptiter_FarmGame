using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct FarmEntityHarvestedPayload
    {
        public readonly string EntityId;
        public readonly Vector3Int Cell;
        public readonly string ProductItemId;
        public readonly int Amount;
        public readonly FarmEntityType EntityType;

        public FarmEntityHarvestedPayload(string entityId, Vector3Int cell, string productItemId, int amount, FarmEntityType entityType)
        {
            EntityId = entityId;
            Cell = cell;
            ProductItemId = productItemId;
            Amount = amount;
            EntityType = entityType;
        }
    }
}
