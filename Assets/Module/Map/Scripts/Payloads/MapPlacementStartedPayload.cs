using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapPlacementStartedPayload
    {
        public readonly int ObjectId;
        public readonly GameObject Prefab;
        public readonly Vector2Int Size;

        public MapPlacementStartedPayload(int objectId, GameObject prefab, Vector2Int size)
        {
            ObjectId = objectId;
            Prefab = prefab;
            Size = size;
        }
    }
}