using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Core.Module.Map
{
    public enum FarmObjectRole
    {
        None = 0,
        Soil = 1,
        Barn = 2
    }

    public enum BuildMenuCategory
    {
        Hidden = 0,
        Resource = 1,
        Decoration = 2
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

    [Flags]
    public enum MapPlacementLayer
    {
        None = 0,
        Surface = 1 << 0,
        Gameplay = 1 << 1,
        SolidDecor = 1 << 2,
        OverlayDecor = 1 << 3
    }

    [Flags]
    public enum MapSurfaceType
    {
        None = 0,
        Land = 1 << 0,
        Water = 1 << 1,
        Any = Land | Water
    }

    [CreateAssetMenu(fileName = "Objects", menuName = "Data/Map/Objects")]
    public class ObjectDatabaseSO : ScriptableObject
    {
        public List<ObjectData> Objects;

        public int PlacedCountRevision { get; private set; }

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

        public bool TryGetFirstByFarmRole(FarmObjectRole role, out ObjectData result)
        {
            if (Objects != null)
            {
                foreach (ObjectData data in Objects)
                {
                    if (data.FarmRole != role) continue;

                    result = data;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public int GetPlacedCount(int objectId)
        {
            return TryGetById(objectId, out ObjectData data, out _)
                ? data.PlacedCount
                : 0;
        }

        public void GetObjectsByMenuCategory(
            BuildMenuCategory category,
            List<ObjectData> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (Objects == null) return;

            foreach (ObjectData data in Objects)
                if (data.MenuCategory == category)
                    results.Add(data);
        }

        internal void ResetPlacedCounts()
        {
            if (Objects == null) return;

            for (int i = 0; i < Objects.Count; i++)
            {
                ObjectData data = Objects[i];
                data.PlacedCount = 0;
                Objects[i] = data;
            }

            PlacedCountRevision++;
        }

        internal void AddPlacedCount(int objectId, int amount)
        {
            if (amount == 0 || !TryGetById(objectId, out ObjectData data, out int index)) return;

            data.PlacedCount = Mathf.Max(0, data.PlacedCount + amount);
            Objects[index] = data;
            PlacedCountRevision++;
        }
    }

    [Serializable]
    public struct ObjectData
    {
        public string name;
        public int ID;
        [Header("Selection UI")]
        [FormerlySerializedAs("SelectionCategory")]
        public BuildMenuCategory MenuCategory;
        public Sprite SelectionIcon;
        public Vector2Int Size;
        [FormerlySerializedAs("Kind")]
        public FarmObjectRole FarmRole;
        public PlacementInputMode PlacementInputMode;
        public PlacementPositionMode PositionMode;
        [Min(0f)] public float FreeSnapStep;
        [Tooltip("Logical collision category. This is independent from Grid/Free positioning.")]
        public MapPlacementLayer PlacementLayer;
        [Tooltip("Placement layers that this object prevents from overlapping its footprint.")]
        public MapPlacementLayer BlockedLayers;
        [Tooltip("Surface created by this object. Use None for objects that do not replace the surface.")]
        public MapSurfaceType ProvidedSurface;
        [Tooltip("Surfaces on which this object may be placed.")]
        public MapSurfaceType AllowedSurfaces;
        public MapObjectRotationMode RotationMode;
        public AssetReferenceGameObject Prefab;

        // Runtime state rebuilt by MapService whenever a map is loaded.
        [NonSerialized] public int PlacedCount;
    }
}
