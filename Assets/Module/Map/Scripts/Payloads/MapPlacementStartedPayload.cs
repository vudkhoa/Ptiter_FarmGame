using UnityEngine;
using UVector2Int = UnityEngine.Vector2Int;

namespace Core.Module.Map
{
    public readonly struct MapPlacementStartedPayload
    {
        public readonly int ObjectId;
        public readonly GameObject Prefab;
        public readonly UVector2Int Size;

        public MapPlacementStartedPayload(int objectId, GameObject prefab, UVector2Int size)
        {
            ObjectId = objectId;
            Prefab = prefab;
            Size = size;
        }
    }
}