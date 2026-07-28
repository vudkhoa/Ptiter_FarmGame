using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapFurnitureAddedPayload
    {
        public readonly int ObjectId;
        public readonly GameObject Prefab;
        public readonly Vector3 SnappedWorld;
        public readonly Vector3Int Cell;
        public readonly int ChangeCount;       // ChangeCount sau khi increment
        public readonly MapObjectRotationMode RotationMode;

        public MapFurnitureAddedPayload(
            int objectId,
            GameObject prefab,
            Vector3 snappedWorld,
            Vector3Int cell,
            int changeCount,
            MapObjectRotationMode rotationMode)
        {
            ObjectId = objectId;
            Prefab = prefab;
            SnappedWorld = snappedWorld;
            Cell = cell;
            ChangeCount = changeCount;
            RotationMode = rotationMode;
        }
    }
}
