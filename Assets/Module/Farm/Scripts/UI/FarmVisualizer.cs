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
        private readonly HashSet<Vector3Int> _pendingPlantCells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _pendingStageCells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _pendingAnimalEnterCells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _pendingAnimalReactionCells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _harvestingCells = new HashSet<Vector3Int>();

        #region DI - Constructor
        [Inject]
        public void Construct(
            IFarmService farmService,
            FarmDatabaseSO database,
            IMapService mapService,
            IMapObjectInstanceRegistry mapObjectRegistry,
            ISubscriber<FarmSlotChangedPayload> slotChangedSub,
            ISubscriber<FarmEntityPlantedPayload> plantedSub,
            ISubscriber<FarmEntityCaredPayload> caredSub,
            ISubscriber<FarmEntityStageChangedPayload> stageChangedSub,
            ISubscriber<FarmEntityRipePayload> ripeSub,
            ISubscriber<FarmEntityHarvestedPayload> harvestedSub)
        {
            _farmService = farmService;
            _database = database;
            _mapService = mapService;
            _mapObjectRegistry = mapObjectRegistry;

            var bag = DisposableBag.CreateBuilder();
            slotChangedSub.Subscribe(OnSlotChanged).AddTo(bag);
            plantedSub.Subscribe(OnPlanted).AddTo(bag);
            caredSub.Subscribe(OnCared).AddTo(bag);
            stageChangedSub.Subscribe(OnStageChanged).AddTo(bag);
            ripeSub.Subscribe(OnRipe).AddTo(bag);
            harvestedSub.Subscribe(OnHarvested).AddTo(bag);
            _subscription = bag.Build();
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
                    EnsurePlacementForSavedSlot(slot);
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

        private void OnPlanted(FarmEntityPlantedPayload payload)
        {
            if (payload.EntityType == FarmEntityType.Crop)
                _pendingPlantCells.Add(payload.Cell);
            else
                _pendingAnimalEnterCells.Add(payload.Cell);
        }

        private void OnCared(FarmEntityCaredPayload payload)
        {
            if (payload.EntityType == FarmEntityType.Animal)
                _pendingAnimalReactionCells.Add(payload.Cell);
        }

        private void OnStageChanged(FarmEntityStageChangedPayload payload)
        {
            if (payload.EntityType == FarmEntityType.Crop)
                _pendingStageCells.Add(payload.Cell);
            else
                _pendingAnimalReactionCells.Add(payload.Cell);
        }

        private void OnRipe(FarmEntityRipePayload payload)
        {
            if (payload.EntityType == FarmEntityType.Crop)
                _pendingStageCells.Add(payload.Cell);
        }

        private void OnHarvested(FarmEntityHarvestedPayload payload)
        {
            if (!_spawnedViews.TryGetValue(payload.Cell, out FarmSlotView view) || view == null)
                return;

            _harvestingCells.Add(payload.Cell);
            Action completeHarvest = () =>
            {
                _harvestingCells.Remove(payload.Cell);
                FarmSlotSaveData currentSlot = _farmService.GetSlotAt(payload.Cell);
                if (currentSlot != null)
                {
                    UpdateVisualSlot(currentSlot);
                }
                else if (_spawnedViews.TryGetValue(payload.Cell, out FarmSlotView staleView))
                {
                    if (staleView != null) Destroy(staleView.gameObject);
                    _spawnedViews.Remove(payload.Cell);
                }
            };

            if (payload.EntityType == FarmEntityType.Animal)
                view.PlayAnimalCollect(completeHarvest);
            else
                view.PlayHarvest(completeHarvest);
        }

        private void UpdateVisualSlot(FarmSlotSaveData slot)
        {
            Vector3Int cell = new Vector3Int(slot.cellX, slot.cellY, slot.cellZ);
            if (_harvestingCells.Contains(cell)) return;

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

            if (_pendingPlantCells.Remove(cell))
                spawnedView.PlayPlant();
            else if (_pendingStageCells.Remove(cell))
                spawnedView.PlayStageChange();
            else if (_pendingAnimalEnterCells.Remove(cell))
                spawnedView.PlayAnimalEnter();
            else if (_pendingAnimalReactionCells.Remove(cell))
                spawnedView.PlayAnimalReaction();

            // Saved slots have no gameplay event to consume. UpdateView starts
            // their idle loop directly, so they intentionally skip the plant pop.
        }

        private void EnsurePlacementForSavedSlot(FarmSlotSaveData slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.entityId)) return;

            FarmEntityData entity = _database.GetEntityById(slot.entityId);
            if (entity == null) return;

            var cell = new Vector3Int(slot.cellX, slot.cellY, slot.cellZ);
            FarmObjectRole role = entity.entityType == FarmEntityType.Animal
                ? FarmObjectRole.Barn
                : FarmObjectRole.Soil;
            _mapService.EnsureFarmPlacement(cell, role);
        }

        private Vector3 ResolveVisualPosition(Vector3Int cell)
        {
            if (_animalAnchors.TryGetValue(cell, out var cachedAnchor))
            {
                if (cachedAnchor != null) return cachedAnchor.transform.position;
                _animalAnchors.Remove(cell);
            }

            if (_mapService.TryGetPlacementAt(cell, out var placement) &&
                placement.FarmRole == FarmObjectRole.Barn &&
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
