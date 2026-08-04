using System;
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
        [SerializeField, Min(0.05f)] private float _freeEraseRadius = 0.75f;

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
        private Vector3 _lastPreviewWorld = new(float.PositiveInfinity, 0f, 0f);
        private readonly Dictionary<string, FreePlacementRecord> _freePlacements = new();

        private IObjectCatalog _catalog;
        private IMapObjectInstanceRegistry _instanceRegistry;
        private MapAuthoringController _authoring;
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
            IMapObjectInstanceRegistry instanceRegistry,
            MapAuthoringController authoring,
            ObjectDatabaseSO database,
            IMapSaveSource saveSource)
        {
            _pubStart = pubStart;
            _pubMove = pubMove;
            _pubAdded = pubAdded;
            _pubStop = pubStop;
            _catalog = catalog;
            _instanceRegistry = instanceRegistry;
            _authoring = authoring;
            _database = database;
            _saveSource = saveSource;
            _persistedPlacements = saveSource?.MapPlacements;
            _authoring.Initialize(this, database);
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
            _mapId = _authoring.Layout != null ? _authoring.Layout.MapId : 0;
        }

        private void Start()
        {
            RestoreLayoutPlacements(_authoring.IsAuthoringMode
                ? _authoring.WorkingEntries
                : _authoring.Layout != null ? _authoring.Layout.Objects : null);

            if (!_authoring.IsAuthoringMode)
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
            _lastPreviewWorld = new Vector3(float.PositiveInfinity, 0f, 0f);

            _pubStart.Publish(new MapPlacementStartedPayload(
                data.ID,
                prefab,
                data.Size,
                data.PositionMode,
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

            var data = _database.Objects[_currentDbIndex];
            Vector3Int cell = WorldToCell(worldHit);
            Vector3 position;
            bool valid;

            if (data.PositionMode == PlacementPositionMode.Free)
            {
                position = GetFreePosition(data, worldHit);
                cell = WorldToCell(position);
                if ((position - _lastPreviewWorld).sqrMagnitude < 0.0001f) return;
                _lastPreviewWorld = position;
                valid = IsPlacementSurfaceValid(cell, Vector2Int.one);
            }
            else
            {
                if (cell == _lastCell) return;
                _lastCell = cell;
                position = CellToWorld(cell);
                valid = _grid.CanPlaceObjectAt(cell, data.Size)
                    && IsPlacementSurfaceValid(cell, data.Size);
            }

            _pubMove.Publish(new MapPreviewMovedPayload(position, cell, valid));
        }

        public bool AddFurniture(Vector3 worldHit)
        {
            if (!HasActivePlacement) return false;

            var data = _database.Objects[_currentDbIndex];
            var cell = WorldToCell(worldHit);

            if (!_catalog.TryGet(data.ID, out var prefab))
            {
                Debug.LogError($"[MapService] Prefab ID {data.ID} is not preloaded in the catalog.");
                return false;
            }

            string instanceId = CreateInstanceId();
            Vector3 placedWorld;

            if (data.PositionMode == PlacementPositionMode.Free)
            {
                placedWorld = GetFreePosition(data, worldHit);
                cell = WorldToCell(placedWorld);
                if (!IsPlacementSurfaceValid(cell, Vector2Int.one) ||
                    !PlaceFreeObjectAt(data, prefab, placedWorld, instanceId)) return false;
            }
            else
            {
                placedWorld = CellToWorld(cell);
                if (!_grid.CanPlaceObjectAt(cell, data.Size) ||
                    !IsPlacementSurfaceValid(cell, data.Size) ||
                    !PlaceGridObjectAt(data, prefab, cell, instanceId)) return false;
            }

            if (_authoring.IsAuthoringMode)
            {
                if (data.PositionMode == PlacementPositionMode.Free)
                    _authoring.RecordFreePlacement(instanceId, data.ID, placedWorld);
                else
                    _authoring.RecordGridPlacement(instanceId, data.ID, cell);
            }
            else
            {
                _persistedPlacements?.Add(new MapPlacementSaveData
                {
                    instanceId = instanceId,
                    objectId = data.ID,
                    positionMode = data.PositionMode,
                    cellX = cell.x,
                    cellY = cell.y,
                    cellZ = cell.z,
                    worldX = placedWorld.x,
                    worldY = placedWorld.y,
                    worldZ = placedWorld.z
                });
                _saveSource?.SaveMap();
            }

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

            string instanceId = CreateInstanceId();
            if (!PlaceGridObjectAt(data, prefab, originCell, instanceId)) return false;

            if (_authoring.IsAuthoringMode)
            {
                _authoring.RecordGridPlacement(instanceId, data.ID, originCell);
            }
            else
            {
                _persistedPlacements?.Add(new MapPlacementSaveData
                {
                    instanceId = instanceId,
                    objectId = data.ID,
                    positionMode = PlacementPositionMode.Grid,
                    cellX = originCell.x,
                    cellY = originCell.y,
                    cellZ = originCell.z
                });
                _saveSource?.SaveMap();
            }
            return true;
        }

        public bool RemoveAuthoringObject(Vector3 worldHit)
        {
            if (!_authoring.IsAuthoringMode) return false;

            if (TryFindFreePlacement(worldHit, out string freeInstanceId))
            {
                _freePlacements.Remove(freeInstanceId);
                _instanceRegistry.RemoveAndDestroy(freeInstanceId);
                _authoring.RecordRemoval(freeInstanceId);
                _changeCount++;
                return true;
            }

            Vector3Int cell = WorldToCell(worldHit);
            if (!_grid.RemoveObjectAt(cell, out PlacementData removed)) return false;
            Vector3Int originCell = removed.OcupiedPositions[0];
            _instanceRegistry.RemoveAndDestroy(originCell);
            _authoring.RecordRemoval(removed.InstanceId);
            _changeCount++;
            return true;
        }

        public void ClearAllPlacements()
        {
            StopPlacement();
            _grid.Clear();
            _freePlacements.Clear();
            _instanceRegistry.ClearAndDestroy();
            _changeCount = 0;
        }

        public void ReloadAuthoringLayout()
        {
            if (!_authoring.IsAuthoringMode) return;
            ClearAllPlacements();
            RestoreLayoutPlacements(_authoring.WorkingEntries);
        }

        private void RestoreLayoutPlacements(IReadOnlyList<MapLayoutEntry> entries)
        {
            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                MapLayoutEntry entry = entries[i];
                if (!_database.TryGetById(entry.ObjectId, out ObjectData data, out _) ||
                    !_catalog.TryGet(entry.ObjectId, out var prefab))
                {
                    Debug.LogWarning($"[MapService] Skipped invalid layout object {entry.ObjectId} at index {i}.");
                    continue;
                }

                string instanceId = EnsureInstanceId(entry.InstanceId);
                bool placed = entry.PositionMode == PlacementPositionMode.Free
                    ? PlaceFreeObjectAt(data, prefab, entry.WorldPosition, instanceId)
                    : PlaceGridObjectAt(data, prefab, entry.OriginCell, instanceId);

                if (!placed)
                    Debug.LogWarning($"[MapService] Skipped overlapping layout object {entry.ObjectId} at index {i}.");
            }
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

                string instanceId = EnsureInstanceId(saved.instanceId);
                if (saved.positionMode == PlacementPositionMode.Free)
                {
                    var worldPosition = new Vector3(saved.worldX, saved.worldY, saved.worldZ);
                    PlaceFreeObjectAt(data, prefab, worldPosition, instanceId);
                    continue;
                }

                var cell = new Vector3Int(saved.cellX, saved.cellY, saved.cellZ);
                if (!PlaceGridObjectAt(data, prefab, cell, instanceId))
                    Debug.LogWarning($"[MapService] Skipped overlapping saved object {saved.objectId} at {cell}.");
            }
        }

        private bool PlaceGridObjectAt(
            ObjectData data,
            GameObject prefab,
            Vector3Int cell,
            string instanceId)
        {
            if (!_grid.CanPlaceObjectAt(cell, data.Size)) return false;

            _grid.AddObjectAt(cell, data.Size, data.ID, data.Kind, _changeCount, instanceId);
            _changeCount++;
            _pubAdded.Publish(new MapFurnitureAddedPayload(
                data.ID,
                prefab,
                CellToWorld(cell),
                cell,
                instanceId,
                PlacementPositionMode.Grid,
                _changeCount,
                data.RotationMode));
            return true;
        }

        private bool PlaceFreeObjectAt(
            ObjectData data,
            GameObject prefab,
            Vector3 worldPosition,
            string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || _freePlacements.ContainsKey(instanceId)) return false;

            _freePlacements.Add(instanceId, new FreePlacementRecord(data.ID, worldPosition));
            _changeCount++;
            _pubAdded.Publish(new MapFurnitureAddedPayload(
                data.ID,
                prefab,
                worldPosition,
                default,
                instanceId,
                PlacementPositionMode.Free,
                _changeCount,
                data.RotationMode));
            return true;
        }

        private Vector3 GetFreePosition(ObjectData data, Vector3 worldHit)
        {
            worldHit.y = 0f;
            float step = data.FreeSnapStep;
            if (step <= 0f) return worldHit;

            worldHit.x = Mathf.Round(worldHit.x / step) * step;
            worldHit.z = Mathf.Round(worldHit.z / step) * step;
            return worldHit;
        }

        private bool TryFindFreePlacement(Vector3 worldHit, out string instanceId)
        {
            instanceId = null;
            float closestSqr = _freeEraseRadius * _freeEraseRadius;
            foreach (KeyValuePair<string, FreePlacementRecord> pair in _freePlacements)
            {
                Vector2 delta = new(pair.Value.WorldPosition.x - worldHit.x, pair.Value.WorldPosition.z - worldHit.z);
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > closestSqr) continue;
                closestSqr = sqrDistance;
                instanceId = pair.Key;
            }
            return !string.IsNullOrEmpty(instanceId);
        }

        private static string CreateInstanceId() => Guid.NewGuid().ToString("N");

        private static string EnsureInstanceId(string instanceId)
        {
            return string.IsNullOrEmpty(instanceId) ? CreateInstanceId() : instanceId;
        }

        private readonly struct FreePlacementRecord
        {
            public readonly int ObjectId;
            public readonly Vector3 WorldPosition;

            public FreePlacementRecord(int objectId, Vector3 worldPosition)
            {
                ObjectId = objectId;
                WorldPosition = worldPosition;
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

        private bool IsPlacementSurfaceValid(Vector3Int cell, Vector2Int size)
        {
            return _authoring.IsAuthoringMode || IsTilemapPlacementValid(cell, size);
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
