// TODO: Recheck UI
using System;
using System.Collections.Generic;
using Core.Module.Map;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Farm
{
    [DisallowMultipleComponent]
    public sealed class FarmVisualizer : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private FarmSlotView _slotViewPrefab;

        [Header("Grid Layout Settings")]
        // Must match Soil.prefab/Visual local position so crops share the same XY plane.
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.6f, 0f);

        private IFarmService _farmService;
        private FarmDatabaseSO _database;
        private IMapService _mapService;
        private IMapObjectInstanceRegistry _mapObjectRegistry;
        private IDisposable _subscription;

        private readonly Dictionary<Vector3Int, FarmSlotView> _spawnedViews = new Dictionary<Vector3Int, FarmSlotView>();
        private readonly Dictionary<Vector3Int, FarmAnimalAnchor> _animalAnchors = new Dictionary<Vector3Int, FarmAnimalAnchor>();

        #region DI - Constructor
        [Inject]
        public void Construct(
            IFarmService farmService,
            FarmDatabaseSO database,
            IMapService mapService,
            IMapObjectInstanceRegistry mapObjectRegistry,
            ISubscriber<FarmSlotChangedPayload> slotChangedSub)
        {
            _farmService = farmService;
            _database = database;
            _mapService = mapService;
            _mapObjectRegistry = mapObjectRegistry;

            // Subscribe to state change events
            _subscription = slotChangedSub.Subscribe(OnSlotChanged);
        }
        #endregion

        #region Unity LifeCycle
        private void Start()
        {
            // Spawn visual slot objects for any already existing saved slots on load
            if (_farmService?.ActiveSlots != null)
            {
                foreach (var slot in _farmService.ActiveSlots)
                {
                    UpdateVisualSlot(slot);
                }
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
        #endregion

        #region Event Callbacks
        private void OnSlotChanged(FarmSlotChangedPayload payload)
        {
            if (payload.Slot != null)
            {
                UpdateVisualSlot(payload.Slot);
            }
        }

        private void UpdateVisualSlot(FarmSlotSaveData slot)
        {
            Vector3Int cell = new Vector3Int(slot.cellX, slot.cellY, slot.cellZ);

            // 1. If the slot is completely empty and unplanted/unoccupied, destroy its visual view
            if (string.IsNullOrEmpty(slot.entityId) && slot.state == FarmSlotState.Empty)
            {
                if (_spawnedViews.TryGetValue(cell, out var view))
                {
                    if (view != null) Destroy(view.gameObject);
                    _spawnedViews.Remove(cell);
                }
                return;
            }

            // 2. Otherwise, spawn the visual slot prefab if not already present
            if (!_spawnedViews.TryGetValue(cell, out var spawnedView) || spawnedView == null)
            {
                Vector3 worldPos = ResolveVisualPosition(cell);
                spawnedView = Instantiate(_slotViewPrefab, worldPos, Quaternion.identity, transform);
                _spawnedViews[cell] = spawnedView;
            }
            else
            {
                spawnedView.transform.position = ResolveVisualPosition(cell);
            }

            // 3. Update the visual states (morphing sprites, sliders, bubbles)
            spawnedView.UpdateView(slot, _database);
        }

        private Vector3 ResolveVisualPosition(Vector3Int cell)
        {
            if (_animalAnchors.TryGetValue(cell, out var cachedAnchor))
            {
                if (cachedAnchor != null) return cachedAnchor.transform.position;
                _animalAnchors.Remove(cell);
            }

            if (_mapService.TryGetPlacementAt(cell, out var placement) &&
                placement.Kind == MapObjectKind.Barn &&
                _mapObjectRegistry.TryGetAtOrigin(cell, out var barnInstance))
            {
                var anchor = barnInstance.GetComponentInChildren<FarmAnimalAnchor>(true);
                if (anchor != null)
                {
                    _animalAnchors[cell] = anchor;
                    return anchor.transform.position;
                }
            }

            return _mapService.CellToWorld(cell) + _offset;
        }
        #endregion
    }
}
