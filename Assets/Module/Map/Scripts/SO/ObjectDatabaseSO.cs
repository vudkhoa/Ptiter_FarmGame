using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Map
{
    public enum MapObjectKind
    {
        Decoration = 0,
        Soil = 1,
        Barn = 2
    }

    public enum MapObjectRotationMode
    {
        KeepPrefabRotation = 0,
        MatchCameraRotation = 1
    }

    public enum PlacementInputMode
    {
        Single = 0,
        Continuous = 1
    }

    public enum PlacementPositionMode
    {
        Grid = 0,
        Free = 1
    }

    [CreateAssetMenu(fileName = "Objects", menuName = "Data/Map/Objects")]
    public class ObjectDatabaseSO : ScriptableObject
    {
        public List<ObjectData> Objects;

        public bool TryGetById(int id, out ObjectData result, out int index)
        {
            if (Objects != null)
            {
                for (int i = 0; i < Objects.Count; i++)
                {
                    if (Objects[i].ID != id) continue;

                    result = Objects[i];
                    index = i;
                    return true;
                }
            }

            result = default;
            index = -1;
            return false;
        }

        public bool TryGetFirstByKind(MapObjectKind kind, out ObjectData result)
        {
            if (Objects != null)
            {
                foreach (ObjectData data in Objects)
                {
                    if (data.Kind != kind) continue;

                    result = data;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }

    [Serializable]
    public struct ObjectData
    {
        public string name;
        public int ID;
        public Vector2Int Size;
        public MapObjectKind Kind;
        public PlacementInputMode PlacementInputMode;
        public PlacementPositionMode PositionMode;
        [Min(0f)] public float FreeSnapStep;
        public MapObjectRotationMode RotationMode;
        public AssetReferenceGameObject Prefab;
    }
}
