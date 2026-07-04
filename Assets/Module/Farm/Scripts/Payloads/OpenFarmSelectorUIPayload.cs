using UnityEngine;

namespace Core.Module.Farm
{
    public readonly struct OpenFarmSelectorUIPayload
    {
        public readonly Vector3Int Cell;
        public readonly bool IsAnimal; // True for animal placement, False for crop placement

        public OpenFarmSelectorUIPayload(Vector3Int cell, bool isAnimal)
        {
            Cell = cell;
            IsAnimal = isAnimal;
        }
    }
}
