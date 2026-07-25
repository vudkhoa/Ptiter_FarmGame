using UnityEngine;

namespace Core.Module.Map
{
    public interface IMapService
    {
        // Query
        int CurrentMapId { get; }
        int ChangeCount { get; }
        int CurrentObjectId { get; }
        bool HasActivePlacement { get; }

        // State machine
        void StartPlacement(int objectId);
        void StopPlacement();

        // World-only API
        void UpdatePreview(Vector3 worldHit);
        bool AddFurniture(Vector3 worldHit);

        // Grid queries & coordinate conversion
        bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data);
        Vector3Int WorldToCell(Vector3 worldPosition);
        Vector3 CellToWorld(Vector3Int cellPosition);
    }
}