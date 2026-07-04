using System;
using Core.Module.Input;
using Core.Module.Map;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Farm
{
    [DisallowMultipleComponent]
    public sealed class FarmInputHandler : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _placeLayer;
        [SerializeField] private float _maxRayDistance = 1000f;

        [Header("Object ID Mappings")]
        [SerializeField] private int _soilId = 101; // ID of Soil/Ruộng from database
        [SerializeField] private int _barnId = 102; // ID of Barn/Chuồng from database

        private IInputService _inputService;
        private IMapService _mapService;
        private IFarmService _farmService;
        private IPublisher<OpenFarmSelectorUIPayload> _openSelectorPub;
        private IDisposable _subscription;

        #region DI - Constructor
        [Inject]
        public void Construct(
            IInputService inputService,
            IMapService mapService,
            IFarmService farmService,
            ISubscriber<PointerButtonDownPayload> clickSub,
            IPublisher<OpenFarmSelectorUIPayload> openSelectorPub)
        {
            _inputService = inputService;
            _mapService = mapService;
            _farmService = farmService;
            _openSelectorPub = openSelectorPub;

            _subscription = clickSub.Subscribe(OnClickDetected);
        }
        #endregion

        #region Unity LifeCycle
        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
        #endregion

        #region Input Click Logic
        private void OnClickDetected(PointerButtonDownPayload payload)
        {
            // Only handle left click / finger tap (Button index 0)
            if (payload.Button != 0) return;

            // Ignore interaction if pointer is over UI elements
            if (_inputService.IsPointerOverUI()) return;

            // If map has active placement (decor/furniture building mode), do not trigger farming interactions
            if (_mapService.HasActivePlacement) return;

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(_inputService.PointerScreen);
            if (Physics.Raycast(ray, out var hit, _maxRayDistance, _placeLayer))
            {
                Vector3Int clickedCell = _mapService.WorldToCell(hit.point);

                if (_mapService.TryGetPlacementAt(clickedCell, out var placement))
                {
                    if (placement.ID == _soilId || placement.ID == _barnId)
                    {
                        // Safely resolve the pivot/origin cell coordinate for large structures (like Barns)
                        if (placement.OcupiedPositions == null || placement.OcupiedPositions.Count == 0) return;
                        
                        Vector3Int originCell = placement.OcupiedPositions[0];
                        bool isAnimal = placement.ID == _barnId;

                        ProcessInteraction(originCell, isAnimal);
                    }
                }
            }
        }

        private void ProcessInteraction(Vector3Int originCell, bool isAnimal)
        {
            var slot = _farmService.GetSlotAt(originCell);

            // 1. If slot is completely empty (no crops planted / no animal bought yet)
            if (slot == null)
            {
                _openSelectorPub.Publish(new OpenFarmSelectorUIPayload(originCell, isAnimal));
                return;
            }

            // 2. Context-aware interaction based on state
            switch (slot.state)
            {
                case FarmSlotState.Empty:
                    if (slot.isAnimal && !slot.isFed)
                    {
                        // Tapping an unfed animal pen triggers FEEDING directly
                        if (_farmService.TryFeed(originCell))
                        {
                            Debug.Log($"[FarmInputHandler] Fed animal at {originCell} successfully.");
                        }
                    }
                    else
                    {
                        // Otherwise open UI selector (e.g. crop seed selector)
                        _openSelectorPub.Publish(new OpenFarmSelectorUIPayload(originCell, isAnimal));
                    }
                    break;

                case FarmSlotState.Growing:
                    // Currently, crops/animals in growing stage cannot be manually interacted with
                    Debug.Log($"[FarmInputHandler] Slot at {originCell} is growing. Time elapsed: {slot.growthTimeSec}s.");
                    break;

                case FarmSlotState.Ripe:
                    // Tapping a ripe crop/animal triggers HARVESTING directly
                    if (_farmService.TryHarvest(originCell, out var product, out var amount))
                    {
                        Debug.Log($"[FarmInputHandler] Harvested {amount}x {product} at {originCell} successfully.");
                    }
                    break;
            }
        }
        #endregion
    }
}
