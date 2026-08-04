using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    [Serializable]
    public class MapPlacementSaveData
    {
        public string instanceId;
        public int objectId;
        public PlacementPositionMode positionMode;
        public int cellX;
        public int cellY;
        public int cellZ;
        public float worldX;
        public float worldY;
        public float worldZ;
    }

    /// <summary>
    /// Save data owned by the application layer and consumed by the scene-scoped map.
    /// Keeping the list instance shared lets map mutations be included in the next player save.
    /// </summary>
    public interface IMapSaveSource
    {
        List<MapPlacementSaveData> MapPlacements { get; }
        void SaveMap();
    }

    public class GridData
    {
        private readonly Dictionary<Vector3Int, PlacementData> _placementObjects = new();

        public void Clear()
        {
            _placementObjects.Clear();
        }

        #region Logic
        public void AddObjectAt(
            Vector3Int gridPosition,
            Vector2Int objectSize,
            int id,
            MapObjectKind kind,
            int placedObjectIndex,
            string instanceId = null)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            PlacementData data = new PlacementData(positionToOccupy, id, kind, placedObjectIndex, instanceId);

            foreach (var pos in positionToOccupy)
            {
                if (_placementObjects.ContainsKey(pos))
                    throw new Exception($"Dictionary already contains key {pos}");
                _placementObjects.Add(pos, data);
            }
        }

        private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector2Int objectSize)
        {
            List<Vector3Int> returnValue = new();
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int z = 0; z < objectSize.y; z++)
                {
                    returnValue.Add(gridPosition + new Vector3Int(x, 0, z));
                }
            }
            return returnValue;
        }

        public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int objectSize)
        {
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int z = 0; z < objectSize.y; z++)
                {
                    if (_placementObjects.ContainsKey(gridPosition + new Vector3Int(x, 0, z)))
                        return false;
                }
            }
            return true;
        }

        public bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data)
        {
            return _placementObjects.TryGetValue(gridPosition, out data);
        }

        public bool RemoveObjectAt(Vector3Int gridPosition, out PlacementData removed)
        {
            if (!_placementObjects.TryGetValue(gridPosition, out removed)) return false;

            foreach (Vector3Int occupiedPosition in removed.OcupiedPositions)
                _placementObjects.Remove(occupiedPosition);
            return true;
        }
        #endregion
    }

    #region Helpers Struct
    [Serializable]
    public struct PlacementData
    {
        public List<Vector3Int> OcupiedPositions;
        public int ID;
        public MapObjectKind Kind;
        public int PlacedObjectIndex;
        public string InstanceId;

        public PlacementData(
            List<Vector3Int> ocupiedPositions,
            int id,
            MapObjectKind kind,
            int placedObjectIndex,
            string instanceId)
        {
            this.OcupiedPositions = ocupiedPositions;
            this.ID = id;
            this.Kind = kind;
            this.PlacedObjectIndex = placedObjectIndex;
            this.InstanceId = instanceId;
        }
    }
    #endregion
}
