// TODO: Recheck UI
using System;
using System.Collections.Generic;
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
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector3 _offset = new Vector3(0.5f, 0.1f, 0.5f); // Centers the sprite on the cell and offsets height

        private IFarmService _farmService;
        private FarmDatabaseSO _database;
        private IDisposable _subscription;

        private readonly Dictionary<Vector3Int, FarmSlotView> _spawnedViews = new Dictionary<Vector3Int, FarmSlotView>();

        #region DI - Constructor
        [Inject]
        public void Construct(
            IFarmService farmService,
            FarmDatabaseSO database,
            ISubscriber<FarmSlotChangedPayload> slotChangedSub)
        {
            _farmService = farmService;
            _database = database;

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
                Vector3 worldPos = new Vector3(cell.x * _cellSize, 0f, cell.z * _cellSize) + _offset;
                spawnedView = Instantiate(_slotViewPrefab, worldPos, Quaternion.identity, transform);
                _spawnedViews[cell] = spawnedView;
            }

            // 3. Update the visual states (morphing sprites, sliders, bubbles)
            spawnedView.UpdateView(slot, _database);
        }
        #endregion
    }
}
