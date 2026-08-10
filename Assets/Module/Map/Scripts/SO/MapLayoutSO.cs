using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    [Serializable]
    public struct MapLayoutEntry
    {
        public string InstanceId;
        public int ObjectId;
        public PlacementPositionMode PositionMode;
        public Vector3Int OriginCell;
        public Vector3 WorldPosition;
        public float UniformScale;

        public static MapLayoutEntry Grid(string instanceId, int objectId, Vector3Int originCell)
        {
            return new MapLayoutEntry
            {
                InstanceId = instanceId,
                ObjectId = objectId,
                PositionMode = PlacementPositionMode.Grid,
                OriginCell = originCell,
                WorldPosition = default,
                UniformScale = 1f
            };
        }

        public static MapLayoutEntry Free(string instanceId, int objectId, Vector3 worldPosition)
        {
            return new MapLayoutEntry
            {
                InstanceId = instanceId,
                ObjectId = objectId,
                PositionMode = PlacementPositionMode.Free,
                OriginCell = default,
                WorldPosition = worldPosition,
                UniformScale = 1f
            };
        }
    }

    [CreateAssetMenu(fileName = "MapLayout", menuName = "Data/Map/Layout")]
    public sealed class MapLayoutSO : ScriptableObject
    {
        public int MapId;
        public List<MapLayoutEntry> Objects = new();
        [Tooltip("Grid cells explicitly blocked for gameplay construction by authored decor.")]
        public List<Vector3Int> DecorBlockedCells = new();
    }
}
