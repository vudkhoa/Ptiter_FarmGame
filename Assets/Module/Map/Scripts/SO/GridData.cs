using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    public class GridData
    {
        private Dictionary<Vector3Int, PlacementData> placementObjects = new();

        #region Logic
        public void AddObjectAt(Vector3Int gridPosition, Vector2Int objectSize, int id, int placedObjectIndex)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            PlacementData data = new PlacementData(positionToOccupy, id, placedObjectIndex);

            foreach (var pos in positionToOccupy)
            {
                if (placementObjects.ContainsKey(pos))
                    throw new Exception($"Dictionary already contains key {pos}");
                placementObjects.Add(pos, data);
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
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            foreach (var pos in positionToOccupy)
                if (placementObjects.ContainsKey(pos))
                    return false;
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
        public int PlacedObjectIndex;

        public PlacementData(List<Vector3Int> ocupiedPositions, int id, int placedObjectIndex)
        {
            this.OcupiedPositions = ocupiedPositions;
            this.ID = id;
            this.PlacedObjectIndex = placedObjectIndex;
        }
    }
    #endregion
}
