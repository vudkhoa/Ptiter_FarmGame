using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapPlacementStartedPayload
    {
        public readonly int ObjectId;
        public readonly GameObject Prefab;
        public readonly Vector2Int Size;
        public readonly PlacementPositionMode PositionMode;
        public readonly MapObjectRotationMode RotationMode;

        public MapPlacementStartedPayload(
            int objectId,
            GameObject prefab,
            Vector2Int size,
            PlacementPositionMode positionMode,
            MapObjectRotationMode rotationMode)
        {
            ObjectId = objectId;
            Prefab = prefab;
            Size = size;
            PositionMode = positionMode;
            RotationMode = rotationMode;
        }
    }
}
