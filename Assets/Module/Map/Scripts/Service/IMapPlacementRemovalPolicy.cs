using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapPlacementRemovalContext
    {
        public readonly string InstanceId;
        public readonly int ObjectId;
        public readonly FarmObjectRole FarmRole;
        public readonly PlacementPositionMode PositionMode;
        public readonly Vector3Int OriginCell;
        public readonly Vector3 WorldPosition;

        public MapPlacementRemovalContext(
            string instanceId,
            int objectId,
            FarmObjectRole farmRole,
            PlacementPositionMode positionMode,
            Vector3Int originCell,
            Vector3 worldPosition)
        {
            InstanceId = instanceId;
            ObjectId = objectId;
            FarmRole = farmRole;
            PositionMode = positionMode;
            OriginCell = originCell;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>
    /// Lets a gameplay module veto removal or clean up its own state without making Map depend
    /// on that module. Implementations are resolved as a collection by MapService.
    /// </summary>
    public interface IMapPlacementRemovalPolicy
    {
        bool CanRemove(in MapPlacementRemovalContext context);
        void OnRemoved(in MapPlacementRemovalContext context);
    }
}
