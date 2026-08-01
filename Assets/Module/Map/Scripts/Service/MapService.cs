using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class MapService : MonoBehaviour, IMapService
    {
        // Injected from root container (registered once in RootLifetimeScope).
        private ObjectDatabaseSO _database;

        [Header("Ref")]
        [SerializeField] private float _cellSize = 1f;

        [Header("Tilemap & Grid Configuration")]
        [SerializeField] private Grid _unityGrid;
        [SerializeField] private List<Tilemap> _buildableTilemaps = new();
        [SerializeField] private List<Tilemap> _obstacleTilemaps = new();

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

        private IObjectCatalog _catalog;
        private IMapSaveSource _saveSource;
        private List<MapPlacementSaveData> _persistedPlacements;

        #region DI - Constructor
        [Inject]
        public void Construct(
            IPublisher<MapPlacementStartedPayload> pubStart,
            IPublisher<MapPreviewMovedPayload> pubMove,
            IPublisher<MapFurnitureAddedPayload> pubAdded,
            IPublisher<MapPlacementStoppedPayload> pubStop,
            IObjectCatalog catalog,
            ObjectDatabaseSO database,
            IMapSaveSource saveSource)
        {
            _pubStart = pubStart;
            _pubMove = pubMove;
            _pubAdded = pubAdded;
            _pubStop = pubStop;
            _catalog = catalog;
            _database = database;
            _saveSource = saveSource;
            _persistedPlacements = saveSource?.MapPlacements;
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (_database == null)
            {
                Debug.LogError($"[MapService] {nameof(_database)} is null - check ObjectDatabaseSO registration in RootLifetimeScope.");
                enabled = false;
                return;
            }

            if (_cellSize <= 0)
            {
                Debug.LogError($"[MapService] _cellSize must be > 0.");
                enabled = false;
                return;
            }

            _grid = new GridData();
            _mapId = 0;
        }

        private void Start()
        {
            RestoreSavedPlacements();
        }
        #endregion

        #region IMapService - Query
        public int CurrentMapId => _mapId;

        public int ChangeCount => _changeCount;

        public int CurrentObjectId => _currentObjectId;

        public bool HasActivePlacement => _currentDbIndex >= 0;

        public PlacementInputMode CurrentPlacementInputMode => HasActivePlacement
            ? _database.Objects[_currentDbIndex].PlacementInputMode
            : PlacementInputMode.Single;

        public bool TryGetPlacementAt(Vector3Int gridPosition, out PlacementData data)
        {
            return _grid.TryGetPlacementAt(gridPosition, out data);
        }
        #endregion

        #region IMapService - State Machine
        public void StartPlacement(int objectId)
        {
            if (HasActivePlacement) StopPlacement();

            if (!_database.TryGetById(objectId, out ObjectData data, out int index))
            {
                Debug.LogError($"ObjectId {objectId} not found");
                return;
            }

            if (!_catalog.TryGet(data.ID, out var prefab))
            {
                Debug.LogError($"[MapService] Prefab ID {data.ID} is not preloaded in the catalog.");
                return;
            }

            _currentObjectId = data.ID;
            _currentDbIndex = index;
            _lastCell = new Vector3Int(int.MinValue, 0, 0);

            _pubStart.Publish(new MapPlacementStartedPayload(
                data.ID,
                prefab,
                data.Size,
                data.RotationMode));
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
            if (!_catalog.TryGet(data.ID, out var prefab))
            {
                Debug.LogError($"[MapService] Prefab ID {data.ID} is not preloaded in the catalog.");
                return false;
            }

            _grid.AddObjectAt(cell, data.Size, data.ID, data.Kind, _changeCount);
            _changeCount++;

            var snapped = CellToWorld(cell);
            _pubAdded.Publish(new MapFurnitureAddedPayload(
                data.ID,
                prefab,
                snapped,
                cell,
                _changeCount,
                data.RotationMode));

            _persistedPlacements?.Add(new MapPlacementSaveData
            {
                objectId = data.ID,
                cellX = cell.x,
                cellY = cell.y,
                cellZ = cell.z
            });
            _saveSource?.SaveMap();

            return true;
        }

        public bool EnsureFarmPlacement(Vector3Int originCell, MapObjectKind kind)
        {
            if (kind != MapObjectKind.Soil && kind != MapObjectKind.Barn) return false;

            if (_grid.TryGetPlacementAt(originCell, out var existing))
                return existing.Kind == kind;

            if (!_database.TryGetFirstByKind(kind, out ObjectData data) ||
                !_catalog.TryGet(data.ID, out var prefab) ||
                !_grid.CanPlaceObjectAt(originCell, data.Size))
            {
                Debug.LogWarning($"[MapService] Could not rebuild missing {kind} at {originCell}.");
                return false;
            }

            _grid.AddObjectAt(originCell, data.Size, data.ID, data.Kind, _changeCount);
            _changeCount++;
            _persistedPlacements?.Add(new MapPlacementSaveData
            {
                objectId = data.ID,
                cellX = originCell.x,
                cellY = originCell.y,
                cellZ = originCell.z
            });

            _pubAdded.Publish(new MapFurnitureAddedPayload(
                data.ID,
                prefab,
                CellToWorld(originCell),
                originCell,
                _changeCount,
                data.RotationMode));
            _saveSource?.SaveMap();
            return true;
        }

        private void RestoreSavedPlacements()
        {
            if (_persistedPlacements == null)
            {
                Debug.LogError("[MapService] No saved map placements are available. Player data may not be loaded yet.");
                return;
            }

            for (int i = 0; i < _persistedPlacements.Count; i++)
            {
                MapPlacementSaveData saved = _persistedPlacements[i];
                if (saved == null ||
                    !_database.TryGetById(saved.objectId, out ObjectData data, out _) ||
                    !_catalog.TryGet(saved.objectId, out var prefab))
                {
                    Debug.LogWarning($"[MapService] Skipped invalid saved placement at index {i}.");
                    continue;
                }

                var cell = new Vector3Int(saved.cellX, saved.cellY, saved.cellZ);
                if (!_grid.CanPlaceObjectAt(cell, data.Size))
                {
                    Debug.LogWarning($"[MapService] Skipped overlapping saved object {saved.objectId} at {cell}.");
                    continue;
                }

                _grid.AddObjectAt(cell, data.Size, data.ID, data.Kind, _changeCount);
                _changeCount++;
                _pubAdded.Publish(new MapFurnitureAddedPayload(
                    data.ID,
                    prefab,
                    CellToWorld(cell),
                    cell,
                    _changeCount,
                    data.RotationMode));
            }
        }

        private bool IsTilemapPlacementValid(Vector3Int cell, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int targetLogicalCell = cell + new Vector3Int(x, 0, z);
                    // Ánh xạ tọa độ logic (x, 0, z) sang tọa độ Unity Grid (x, z, y) do swizzle XZY
                    Vector3Int unityCell = new Vector3Int(targetLogicalCell.x, targetLogicalCell.z, targetLogicalCell.y);

                    // 1. Kiểm tra Buildable: Phải có ít nhất 1 Tilemap trong danh sách chứa gạch tại đây
                    if (_buildableTilemaps != null && _buildableTilemaps.Count > 0)
                    {
                        bool hasValidBase = false;
                        foreach (var tilemap in _buildableTilemaps)
                        {
                            if (tilemap != null && tilemap.GetTile(unityCell) != null)
                            {
                                hasValidBase = true;
                                break;
                            }
                        }
                        if (!hasValidBase) return false; // Không có gạch nền nào đỡ ở dưới
                    }

                    // 2. Kiểm tra Obstacle: Không được phép có gạch của bất kỳ Tilemap chướng ngại vật nào
                    if (_obstacleTilemaps != null && _obstacleTilemaps.Count > 0)
                    {
                        foreach (var tilemap in _obstacleTilemaps)
                        {
                            if (tilemap != null && tilemap.GetTile(unityCell) != null)
                            {
                                return false; // Dính chướng ngại vật
                            }
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
