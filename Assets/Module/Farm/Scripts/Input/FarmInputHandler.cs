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
        [SerializeField] private bool _useMathPlane = true;

        [Header("Tap Gesture")]
        [SerializeField, Min(1f)] private float _tapMaxMovementPixels = 20f;

        private IInputService _inputService;
        private IMapService _mapService;
        private IFarmService _farmService;
        private IPublisher<OpenFarmSelectorUIPayload> _openSelectorPub;
        private Vector2 _tapStartScreen;
        private bool _isTapTracking;
        private bool _tapMovedTooFar;
        private IDisposable _subscriptions;

        #region DI - Constructor
        [Inject]
        public void Construct(
            IInputService inputService,
            IMapService mapService,
            IFarmService farmService,
            ISubscriber<PointerScreenPayload> screenSub,
            ISubscriber<PointerButtonDownPayload> buttonDownSub,
            ISubscriber<PointerButtonUpPayload> buttonUpSub,
            IPublisher<OpenFarmSelectorUIPayload> openSelectorPub)
        {
            _inputService = inputService;
            _mapService = mapService;
            _farmService = farmService;
            _openSelectorPub = openSelectorPub;

            var bag = DisposableBag.CreateBuilder();
            screenSub.Subscribe(OnPointerScreen).AddTo(bag);
            buttonDownSub.Subscribe(OnPointerDown).AddTo(bag);
            buttonUpSub.Subscribe(OnPointerUp).AddTo(bag);
            _subscriptions = bag.Build();
        }
        #endregion

        #region Unity LifeCycle
        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
        #endregion

        #region Input Click Logic
        private void OnPointerDown(PointerButtonDownPayload payload)
        {
            if (payload.Button != 0) return;

            _isTapTracking = false;
            if (_inputService.IsPointerOverUI()) return;
            if (_mapService.HasActivePlacement || _mapService.IsPlayerRemovalMode) return;

            _tapStartScreen = _inputService.PointerScreen;
            _tapMovedTooFar = false;
            _isTapTracking = true;
        }

        private void OnPointerScreen(PointerScreenPayload payload)
        {
            if (!_isTapTracking || _tapMovedTooFar) return;

            float maxMovementSqr = _tapMaxMovementPixels * _tapMaxMovementPixels;
            if ((payload.ScreenPosition - _tapStartScreen).sqrMagnitude > maxMovementSqr)
                _tapMovedTooFar = true;
        }

        private void OnPointerUp(PointerButtonUpPayload payload)
        {
            if (payload.Button != 0 || !_isTapTracking) return;

            bool isTap = !_tapMovedTooFar;
            _isTapTracking = false;
            _tapMovedTooFar = false;

            if (!isTap || _inputService.IsPointerOverUI()) return;
            if (_mapService.HasActivePlacement || _mapService.IsPlayerRemovalMode) return;

            ProcessTap(_inputService.PointerScreen);
        }

        private void ProcessTap(Vector2 screenPosition)
        {
            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            Vector3 hitPoint;
            bool hasHit = false;

            var ray = cam.ScreenPointToRay(screenPosition);
            if (_useMathPlane)
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                    hasHit = true;
                }
                else
                {
                    hitPoint = default;
                }
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, _maxRayDistance, _placeLayer))
                {
                    hitPoint = hit.point;
                    hasHit = true;
                }
                else
                {
                    hitPoint = default;
                }
            }

            if (hasHit)
            {
                Vector3Int clickedCell = _mapService.WorldToCell(hitPoint);

                if (_mapService.TryGetPlacementAt(clickedCell, out var placement))
                {
                    bool? isAnimal = placement.Kind switch
                    {
                        MapObjectKind.Soil => false,
                        MapObjectKind.Barn => true,
                        _ => null
                    };

                    if (isAnimal.HasValue)
                    {
                        // Resolve the origin cell for multi-cell farm structures.
                        if (placement.OcupiedPositions == null || placement.OcupiedPositions.Count == 0) return;

                        Vector3Int originCell = placement.OcupiedPositions[0];
                        ProcessInteraction(originCell, isAnimal.Value);
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
                    if (isAnimal && !string.IsNullOrEmpty(slot.entityId) && !slot.isFed)
                    {
                        // Tapping an unfed animal pen (with a purchased animal) triggers FEEDING directly
                        _farmService.TryFeed(originCell);
                    }
                    else
                    {
                        // Otherwise open UI selector (e.g. crop seed selector or animal shop UI)
                        _openSelectorPub.Publish(new OpenFarmSelectorUIPayload(originCell, isAnimal));
                    }
                    break;

                case FarmSlotState.Growing:
                    break;

                case FarmSlotState.Ripe:
                    // Tapping a ripe crop/animal triggers HARVESTING directly
                    _farmService.TryHarvest(originCell, out _, out _);
                    break;
            }
        }
        #endregion
    }
}
