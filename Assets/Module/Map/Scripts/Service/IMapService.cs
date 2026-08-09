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
        bool IsPlayerRemovalMode { get; }
        PlacementInputMode CurrentPlacementInputMode { get; }

        // State machine
        void StartPlacement(int objectId);
        void StopPlacement();
        void SetPlayerRemovalMode(bool active);

        // World-only API
        void UpdatePreview(Vector3 worldHit);
        bool AddFurniture(Vector3 worldHit);
        bool RemovePlayerObject(Vector3 worldHit);
        bool RemoveAuthoringObject(Vector3 worldHit);
        bool SelectAuthoringObject(Vector3 worldHit);
        bool MoveSelectedAuthoringObject(Vector3 worldHit);
        void SetSelectedAuthoringScale(float uniformScale);

        // Legacy-save compatibility: rebuild a missing Soil/Barn underneath a persisted farm slot.
        bool EnsureFarmPlacement(Vector3Int originCell, MapObjectKind kind);

        // Grid queries & coordinate conversion
        bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data);
        bool CanRemovePlayerObject(Vector3 worldHit);
        Vector3Int WorldToCell(Vector3 worldPosition);
        Vector3 CellToWorld(Vector3Int cellPosition);
    }
}
