using MessagePipe;
using UnityEngine;

namespace Core.Module.Map
{
    // Block Conflict Component
    [DisallowMultipleComponent]
    public sealed class MapService : MonoBehaviour, IMapService
    {
        [Header("Ref")]
        [SerializeField] private ObjectDatabaseSO _database;
        [SerializeField] private float _cellSize = 1f;

        private IPublisher<MapPlacementStartedPayload> _pubStart;
        private IPublisher<MapPreviewModePayload> _pubMove;
        private IPublisher<MapFurnitureAddedPayload> _pubAdded;
        private IPublisher<MapPlacementStopedPayload> _pubStop;


        public int CurrentMapId => throw new System.NotImplementedException();

        public int ChangeCount => throw new System.NotImplementedException();

        public int CurrentObjectId => throw new System.NotImplementedException();

        public bool HasActivePlacement => throw new System.NotImplementedException();

        public bool AddFurniture(Vector3 worldHit)
        {
            throw new System.NotImplementedException();
        }

        public void StartPlacement(int objectId)
        {
            throw new System.NotImplementedException();
        }

        public void StopPlacement()
        {
            throw new System.NotImplementedException();
        }

        public void UpdatePreview(Vector3 worldHit)
        {
            throw new System.NotImplementedException();
        }
    }
}