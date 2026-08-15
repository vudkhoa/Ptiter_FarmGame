using UnityEngine;

namespace Core.Module.Map
{
    public interface IMapPlacementPaymentService
    {
        int Balance { get; }

        bool TrySpend(int objectId, int amount);
        bool Refund(int objectId, int amount);
    }

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

        /// <paramref name="reopenPicker"/> false ends the placement without inviting the build
        /// menu back, for a stop the game performed rather than the player.
        void StopPlacement(bool reopenPicker);
        void SetPlayerRemovalMode(bool active);

        // World-only API
        void UpdatePreview(Vector3 worldHit);
        bool AddFurniture(Vector3 worldHit);
        bool RemovePlayerObject(Vector3 worldHit);
        bool RemoveAuthoringObject(Vector3 worldHit);
        bool SelectAuthoringObject(Vector3 worldHit);
        bool MoveSelectedAuthoringObject(Vector3 worldHit);
        void SetSelectedAuthoringScale(float uniformScale);
        bool SetDecorBlocker(Vector3 worldHit, bool blocked);
        void ClearAllDecorBlockers();

        // Legacy-save compatibility: rebuild a missing Soil/Barn underneath a persisted farm slot.
        bool EnsureFarmPlacement(Vector3Int originCell, FarmObjectRole role);

        // Grid queries & coordinate conversion
        bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data);

        // Grid Block Check
        bool TryGetBlockerAt(Vector3Int gridPosition);

        /// Would placing <paramref name="objectId"/> at this cell succeed right now? Same checks
        /// AddFurniture runs, without committing anything.
        bool CanPlaceObjectAt(int objectId, Vector3Int gridPosition);
        bool CanRemovePlayerObject(Vector3 worldHit);
        Vector3Int WorldToCell(Vector3 worldPosition);
        Vector3 CellToWorld(Vector3Int cellPosition);
    }
}
