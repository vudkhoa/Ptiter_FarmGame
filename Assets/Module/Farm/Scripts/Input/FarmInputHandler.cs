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
        private PointerTapTracker _tapTracker;
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
        private void Awake()
        {
            _tapTracker = new PointerTapTracker(_tapMaxMovementPixels);
        }

        private void Update()
        {
            if (_inputService != null &&
                (_inputService.IsMultiTouchActive ||
                 _inputService.IsGameplayInputBlocked))
                _tapTracker.Cancel();
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
        #endregion

        #region Input Click Logic
        private void OnPointerDown(PointerButtonDownPayload payload)
        {
            if (payload.Button != 0) return;

            _tapTracker.Cancel();

            // Full-screen modal windows such as Quest and Inventory own input
            // while they are open. Gameplay windows (for example the farm
            // selector) deliberately do not register as blockers.
            if (_inputService.IsGameplayInputBlocked) return;
            if (_inputService.IsPointerOverUI()) return;
            if (_mapService.HasActivePlacement || _mapService.IsPlayerRemovalMode) return;

            _tapTracker.Begin(_inputService.PointerScreen);
        }

        private void OnPointerScreen(PointerScreenPayload payload)
        {
            if (_inputService.IsGameplayInputBlocked)
            {
                _tapTracker.Cancel();
                return;
            }

            _tapTracker.Move(payload.ScreenPosition);
        }

        private void OnPointerUp(PointerButtonUpPayload payload)
        {
            if (payload.Button != 0) return;
            if (PointerReleaseSuppression.IsPrimaryReleaseSuppressed)
            {
                _tapTracker.Cancel();
                return;
            }
            if (_inputService.IsGameplayInputBlocked)
            {
                _tapTracker.Cancel();
                return;
            }

            if (!_tapTracker.Complete(_inputService.PointerScreen)) return;
            if (_inputService.IsPointerOverUI()) return;
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
                    bool? isAnimal = placement.FarmRole switch
                    {
                        FarmObjectRole.Soil => false,
                        FarmObjectRole.Barn => true,
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
