using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapPreviewModePayload
    {
        public readonly Vector3 SnappedWorld;     // CellToWorld(cell), Y=0
        public readonly Vector3Int Cell;
        public readonly bool IsValid;

        public MapPreviewModePayload(Vector3 snappedWorld, Vector3Int cell, bool isValid)
        {
            SnappedWorld = snappedWorld;
            Cell = cell;
            IsValid = isValid;
        }
    }
}