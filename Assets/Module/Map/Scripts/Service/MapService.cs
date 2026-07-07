using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapService : MonoBehaviour, IMapService
    {
        [Header("Ref")]
        [SerializeField] private ObjectDatabaseSO _database;
        [SerializeField] private float _cellSize = 1f;

        [Header("Tilemap & Grid Configuration")]
        [SerializeField] private Grid _unityGrid;
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private List<string> _forbiddenTileKeywords = new() { "water", "void" };

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

        public bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data)
        {
            return _grid.TryGetPlacementAt(gridPosition, out data);
        }
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
            bool valid = _grid.CanPlaceObjectAt(cell, data.Size) && IsTilemapPlacementValid(cell, data.Size);
            var snapped = CellToWorld(cell);

            _pubMove.Publish(new MapPreviewMovedPayload(snapped, cell, valid));
        }

        public bool AddFurniture(Vector3 worldHit)
        {
            if (!HasActivePlacement) return false;

            var cell = WorldToCell(worldHit);
            var data = _database.Objects[_currentDbIndex];

            if (!_grid.CanPlaceObjectAt(cell, data.Size) || !IsTilemapPlacementValid(cell, data.Size)) return false;

            _grid.AddObjectAt(cell, data.Size, data.ID, _changeCount);
            _changeCount++;

            var snapped = CellToWorld(cell);
            _pubAdded.Publish(new MapFurnitureAddedPayload(data.ID, data.Prefab, snapped, cell, _changeCount));

            return true;
        }

        private bool IsTilemapPlacementValid(Vector3Int cell, Vector2Int size)
        {
            if (_groundTilemap == null) return true;
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int targetLogicalCell = cell + new Vector3Int(x, 0, z);
                    // Ánh xạ tọa độ logic (x, 0, z) sang tọa độ Unity Grid (x, z, y) do swizzle XZY
                    Vector3Int unityCell = new Vector3Int(targetLogicalCell.x, targetLogicalCell.z, targetLogicalCell.y);
                    TileBase tile = _groundTilemap.GetTile(unityCell);
                    if (tile == null) return false; // Không được đặt lên ô trống vô định

                    string nameLower = tile.name.ToLower();
                    foreach (var keyword in _forbiddenTileKeywords)
                    {
                        if (nameLower.Contains(keyword.ToLower()))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        #endregion

        #region Cell math
        public Vector3Int WorldToCell(Vector3 w)
        {
            if (_unityGrid != null)
            {
                Vector3Int unityCell = _unityGrid.WorldToCell(w);
                // Unity Grid XZY trả về (x, z, y) của hệ tọa độ thế giới.
                // Chúng ta ánh xạ về dạng logic của game: (x, 0, z).
                return new Vector3Int(unityCell.x, 0, unityCell.y);
            }
            return new Vector3Int(
                Mathf.FloorToInt(w.x / _cellSize),
                0,
                Mathf.FloorToInt(w.z / _cellSize)
            );
        }

        public Vector3 CellToWorld(Vector3Int c)
        {
            if (_unityGrid != null)
            {
                // Ánh xạ tọa độ logic (x, 0, z) sang tọa độ Unity Grid (x, z, y) trước khi chuyển đổi
                Vector3Int unityCell = new Vector3Int(c.x, c.z, c.y);
                return _unityGrid.CellToWorld(unityCell);
            }
            return new Vector3(
                c.x * _cellSize,
                0f,
                c.z * _cellSize
            );
        }
        #endregion
    }
}
