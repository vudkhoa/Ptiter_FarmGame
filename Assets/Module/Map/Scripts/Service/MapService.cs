using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapService : MonoBehaviour, IMapService
    {
        [Header("Ref")]
        [SerializeField] private ObjectDatabaseSO _database;
        [SerializeField] private float _cellSize = 1f;

        private IPublisher<MapPlacementStartedPayload> _pubStart;
        private IPublisher<MapPreviewMovedPayload> _pubMove;
        private IPublisher<MapFurnitureAddedPayload> _pubAdded;
        private IPublisher<MapPlacementStoppedPayload> _pubStop;

        private GridData _grid;
        private int _mapId;
        private int _currentObjectId = -1;
        private int _currentDbIndex = -1;
        private int _changeCount;
        private Vector3Int _lastCell = new(int.MinValue, 0, 0);

        #region DI - Constructor
        [Inject]
        public void Construct(
            IPublisher<MapPlacementStartedPayload> pubStart,
            IPublisher<MapPreviewMovedPayload> pubMove,
            IPublisher<MapFurnitureAddedPayload> pubAdded,
            IPublisher<MapPlacementStoppedPayload> pubStop)
        {
            _pubStart = pubStart;
            _pubMove = pubMove;
            _pubAdded = pubAdded;
            _pubStop = pubStop;
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (_database == null)
            {
                Debug.LogError($"[MapService] {nameof(_database)} null — drag ObjectDatabaseSO vào Inspector.");
                enabled = false;
                return;
            }

            if (_cellSize <= 0)
            {
                Debug.LogError($"[MapService] _cellSize phải > 0.");
                enabled = false;
                return;
            }

            _grid = new GridData();
            _mapId = 0;
        }
        #endregion

        #region IMapService - Query
        public int CurrentMapId => _mapId;

        public int ChangeCount => _changeCount;

        public int CurrentObjectId => _currentObjectId;

        public bool HasActivePlacement => _currentDbIndex >= 0;
        #endregion

        #region IMapService - State Machine
        public void StartPlacement(int objectId)
        {
            if (HasActivePlacement) StopPlacement();

            int idx = -1;
            for (int i = 0; i < _database.Objects.Count; ++i)
            {
                var obj = _database.Objects[i];
                if (obj.ID == objectId)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                Debug.LogError($"ObjectId {objectId} not found");
                return;
            }

            var data = _database.Objects[idx];
            _currentObjectId = data.ID;
            _currentDbIndex = idx;
            _lastCell = new Vector3Int(int.MinValue, 0, 0);

            _pubStart.Publish(new MapPlacementStartedPayload(data.ID, data.Prefab, data.Size));
        }

        public void StopPlacement()
        {
            if (!HasActivePlacement) return;
            _currentObjectId = -1;
            _currentDbIndex = -1;
            _pubStop.Publish(default);
        }
        #endregion

        #region IMapService - Placement Actions
        public void UpdatePreview(Vector3 worldHit)
        {
            if (!HasActivePlacement) return;

            var cell = WorldToCell(worldHit);
            if (cell == _lastCell) return;
            _lastCell = cell;

            var data = _database.Objects[_currentDbIndex];
            bool valid = _grid.CanPlaceObjectAt(cell, data.Size);
            var snapped = CellToWorld(cell);

            _pubMove.Publish(new MapPreviewMovedPayload(snapped, cell, valid));
        }

        public bool AddFurniture(Vector3 worldHit)
        {
            if (!HasActivePlacement) return false;

            var cell = WorldToCell(worldHit);
            var data = _database.Objects[_currentDbIndex];

            if (!_grid.CanPlaceObjectAt(cell, data.Size)) return false;

            _grid.AddObjectAt(cell, data.Size, data.ID, _changeCount);
            _changeCount++;

            var snapped = CellToWorld(cell);
            _pubAdded.Publish(new MapFurnitureAddedPayload(data.ID, data.Prefab, snapped, cell, _changeCount));

            return true;
        }
        #endregion

        #region Cell math
        private Vector3Int WorldToCell(Vector3 w) => new Vector3Int(
            Mathf.FloorToInt(w.x / _cellSize),
            0,
            Mathf.FloorToInt(w.z / _cellSize)
        );

        private Vector3 CellToWorld(Vector3Int c) => new Vector3(
            c.x * _cellSize,
            0f,
            c.z * _cellSize
        );
        #endregion
    }
}
