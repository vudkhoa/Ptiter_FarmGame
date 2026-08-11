using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    /// <summary>
    /// Evaluates logical placement rules. It owns no placement state and performs no spawning,
    /// persistence, authoring updates, or Tilemap validation.
    /// </summary>
    internal sealed class MapPlacementValidator
    {
        private readonly ObjectDatabaseSO _database;
        private readonly GridData _grid;
        private readonly IReadOnlyDictionary<string, FreePlacementRecord> _freePlacements;
        private readonly Func<Vector3, Vector3Int> _worldToCell;
        private readonly Func<Vector3Int, bool> _isDecorBlocked;

        public MapPlacementValidator(
            ObjectDatabaseSO database,
            GridData grid,
            IReadOnlyDictionary<string, FreePlacementRecord> freePlacements,
            Func<Vector3, Vector3Int> worldToCell,
            Func<Vector3Int, bool> isDecorBlocked)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _freePlacements = freePlacements ?? throw new ArgumentNullException(nameof(freePlacements));
            _worldToCell = worldToCell ?? throw new ArgumentNullException(nameof(worldToCell));
            _isDecorBlocked = isDecorBlocked ?? throw new ArgumentNullException(nameof(isDecorBlocked));
        }

        public bool CanPlaceGrid(ObjectData data, Vector3Int originCell)
        {
            Vector2Int size = NormalizeSize(data.Size);

            if (IsGameplayBlockedByDecor(data, originCell, size)) return false;

            // GridData currently supports one Grid placement per cell. Grid-vs-Grid therefore
            // remains exclusive even when the layer masks would otherwise allow an overlap.
            if (!_grid.CanPlaceObjectAt(originCell, size) ||
                !IsAllowedOnSurfaces(data, originCell, size)) return false;

            foreach (FreePlacementRecord free in _freePlacements.Values)
            {
                if (!_database.TryGetById(free.ObjectId, out ObjectData freeData, out _) ||
                    !DoLayersConflict(data, freeData)) continue;

                Vector3Int freeOrigin = _worldToCell(free.WorldPosition);
                if (DoFootprintsOverlap(originCell, size, freeOrigin, NormalizeSize(freeData.Size)))
                    return false;
            }

            return true;
        }

        public bool CanPlaceFree(ObjectData data, Vector3 worldPosition)
        {
            Vector3Int originCell = _worldToCell(worldPosition);
            Vector2Int size = NormalizeSize(data.Size);
            if (!IsAllowedOnSurfaces(data, originCell, size)) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int cell = originCell + new Vector3Int(x, 0, z);
                    if (!_grid.TryGetPlacementAt(cell, out PlacementData existing)) continue;
                    if (!_database.TryGetById(existing.ID, out ObjectData existingData, out _) ||
                        DoLayersConflict(data, existingData)) return false;
                }
            }

            // Free-vs-Free overlap is intentionally allowed by the current authoring rules.
            return true;
        }

        private bool IsAllowedOnSurfaces(ObjectData data, Vector3Int originCell, Vector2Int size)
        {
            // None is treated as Any for compatibility with ObjectData serialized before
            // AllowedSurfaces was introduced. New entries should always configure this field.
            MapSurfaceType allowed = data.AllowedSurfaces == MapSurfaceType.None
                ? MapSurfaceType.Any
                : data.AllowedSurfaces;

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    MapSurfaceType surface = GetSurfaceAt(originCell + new Vector3Int(x, 0, z));
                    if ((allowed & surface) == 0) return false;
                }
            }

            return true;
        }

        private bool IsGameplayBlockedByDecor(ObjectData data, Vector3Int originCell, Vector2Int size)
        {
            if ((data.PlacementLayer & MapPlacementLayer.Gameplay) == 0) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    if (_isDecorBlocked(originCell + new Vector3Int(x, 0, z))) return true;
                }
            }

            return false;
        }

        private MapSurfaceType GetSurfaceAt(Vector3Int cell)
        {
            if (_grid.TryGetPlacementAt(cell, out PlacementData placement) &&
                _database.TryGetById(placement.ID, out ObjectData data, out _) &&
                data.ProvidedSurface != MapSurfaceType.None)
                return data.ProvidedSurface;

            return MapSurfaceType.Land;
        }

        private static bool DoLayersConflict(ObjectData first, ObjectData second)
        {
            return (first.BlockedLayers & second.PlacementLayer) != 0 ||
                   (second.BlockedLayers & first.PlacementLayer) != 0;
        }

        private static bool DoFootprintsOverlap(
            Vector3Int firstOrigin,
            Vector2Int firstSize,
            Vector3Int secondOrigin,
            Vector2Int secondSize)
        {
            return firstOrigin.x < secondOrigin.x + secondSize.x
                && firstOrigin.x + firstSize.x > secondOrigin.x
                && firstOrigin.z < secondOrigin.z + secondSize.y
                && firstOrigin.z + firstSize.y > secondOrigin.z;
        }

        private static Vector2Int NormalizeSize(Vector2Int size)
        {
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }
    }

    internal struct FreePlacementRecord
    {
        public readonly int ObjectId;
        public Vector3 WorldPosition;
        public float UniformScale;

        public FreePlacementRecord(int objectId, Vector3 worldPosition, float uniformScale)
        {
            ObjectId = objectId;
            WorldPosition = worldPosition;
            UniformScale = uniformScale;
        }
    }
}
