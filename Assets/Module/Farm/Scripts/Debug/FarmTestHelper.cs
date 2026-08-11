#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;
using Core.Module.Map;
using UnityEngine;
using VContainer;

namespace Core.Module.Farm
{
    /// <summary>Adds optional farm placements for editor and development testing.</summary>
    [DisallowMultipleComponent]
    public sealed class FarmTestHelper : MonoBehaviour
    {
        private IMapService _mapService;
        private ObjectDatabaseSO _objectDatabase;

        [Inject]
        public void Construct(IMapService mapService, ObjectDatabaseSO objectDatabase)
        {
            _mapService = mapService;
            _objectDatabase = objectDatabase;
        }

        private void Start()
        {
            if (_mapService is not MapService concreteMapService || _objectDatabase == null) return;

            try
            {
                var gridField = typeof(MapService).GetField(
                    "_grid",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var grid = gridField?.GetValue(concreteMapService) as GridData;
                if (grid == null) return;

                AddTestPlacement(grid, FarmObjectRole.Soil, new Vector3Int(0, 0, 0), 999);
                AddTestPlacement(grid, FarmObjectRole.Barn, new Vector3Int(3, 0, 3), 998);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FarmTestHelper] Failed to create test placements: {exception.Message}");
            }
        }

        private void AddTestPlacement(
            GridData grid,
            FarmObjectRole role,
            Vector3Int origin,
            int placedObjectIndex)
        {
            if (!TryGetObject(role, out ObjectData data))
            {
                Debug.LogWarning($"[FarmTestHelper] No {role} entry exists in ObjectDatabaseSO; test placement skipped.");
                return;
            }

            grid.AddObjectAt(origin, data.Size, data.ID, data.FarmRole, placedObjectIndex);
        }

        private bool TryGetObject(FarmObjectRole role, out ObjectData result)
        {
            return _objectDatabase.TryGetFirstByFarmRole(role, out result);
        }
    }
}
#endif
