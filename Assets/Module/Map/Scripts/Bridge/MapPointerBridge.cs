using System;
using Core.Module.Input;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Map
{
    /// <summary>
    /// - Bridge: Sub.
    /// - InputService: Call Payload.
    /// - Bridge: Setup + Call Function.
    /// - MapService: Define Function.
    /// </summary>

    [DisallowMultipleComponent]
    public sealed class MapPointerBridge : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _placeLayer;
        [SerializeField] private float _maxRayDistance = 1000f;
        [SerializeField] private bool _useMathPlane = true;

        [Header("Tap Gesture")]
        [SerializeField, Min(1f)] private float _removalTapMaxMovementPixels = 20f;

        [Header("Remove Option")]
        [SerializeField, Min(0.1f)] private float _removeOptionHoldSeconds = 0.55f;

        private IMapService _map;
        private IInputService _input;
        private MapAuthoringController _authoring;
        private Vector2 _lastScreen;
        private bool _isPrimaryPressed;
        private PointerTapTracker _removalTapTracker;
        private IPublisher<MapPlayerRemoveOptionPayload> _removeOptionPublisher;
        private bool _isRemoveOptionHoldTracking;
        private bool _didShowRemoveOption;
        private float _removeOptionHoldStartedAt;
        private Vector2 _removeOptionHoldScreen;
        private Vector3 _removeOptionHoldWorld;
        private IDisposable _subscriptions;

        #region DI - Constructor
        [Inject]
        public void Construct(
           IMapService map,
           IInputService input,
           MapAuthoringController authoring,
           ISubscriber<PointerScreenPayload> screenSub,
           ISubscriber<PointerButtonDownPayload> btnDownSub,
           ISubscriber<PointerButtonUpPayload> btnUpSub,
           ISubscriber<KeyDownPayload> keySub,
           IPublisher<MapPlayerRemoveOptionPayload> removeOptionPublisher)
        {
            _map = map;
            _input = input;
            _authoring = authoring;
            _removeOptionPublisher = removeOptionPublisher;

            var bag = DisposableBag.CreateBuilder();
            screenSub.Subscribe(OnScreen).AddTo(bag);
            btnDownSub.Subscribe(OnButtonDown).AddTo(bag);
            btnUpSub.Subscribe(OnButtonUp).AddTo(bag);
            keySub.Subscribe(OnKey).AddTo(bag);
            _subscriptions = bag.Build();
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            _removalTapTracker = new PointerTapTracker(_removalTapMaxMovementPixels);
        }

        private void Update()
        {
            if (_input != null &&
                (_input.IsMultiTouchActive || _input.IsGameplayInputBlocked))
            {
                _removalTapTracker.Cancel();
                CancelRemoveOptionHold(true);
                return;
            }

            if (!_isRemoveOptionHoldTracking || _didShowRemoveOption ||
                Time.unscaledTime - _removeOptionHoldStartedAt < _removeOptionHoldSeconds) return;

            _didShowRemoveOption = true;
            if (_map.CanRemovePlayerObject(_removeOptionHoldWorld))
            {
                PointerReleaseSuppression.SuppressPrimaryRelease();
                Vector2 optionScreenPosition = GetCellScreenCenter(_removeOptionHoldWorld);
                _removeOptionPublisher.Publish(new MapPlayerRemoveOptionPayload(
                    true,
                    optionScreenPosition,
                    _removeOptionHoldWorld));
            }
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
        #endregion

        #region Sub Logic
        private void OnScreen(PointerScreenPayload p)
        {
            _lastScreen = p.ScreenPosition;
            if (_input.IsGameplayInputBlocked)
            {
                _isPrimaryPressed = false;
                _removalTapTracker.Cancel();
                CancelRemoveOptionHold(true);
                return;
            }

            if (_isRemoveOptionHoldTracking &&
                (_lastScreen - _removeOptionHoldScreen).sqrMagnitude >
                _removalTapMaxMovementPixels * _removalTapMaxMovementPixels)
            {
                CancelRemoveOptionHold(true);
            }

            if (_map.IsPlayerRemovalMode)
            {
                _removalTapTracker.Move(_lastScreen);
                return;
            }

            if (_authoring != null && _authoring.IsSelectMode && _isPrimaryPressed)
            {
                if (!IsPointerBlocked() && TryRaycast(_lastScreen, out var moveWorld))
                    _map.MoveSelectedAuthoringObject(moveWorld);
                return;
            }

            if (_authoring != null && _authoring.IsDecorBlockerMode)
            {
                if (_isPrimaryPressed && !IsPointerBlocked() && TryRaycast(_lastScreen, out var blockerWorld))
                    _map.SetDecorBlocker(blockerWorld, _authoring.IsDecorBlockerPaintMode);
                return;
            }

            if (!_map.HasActivePlacement) return;
            if (!TryRaycast(_lastScreen, out var world)) return;

            _map.UpdatePreview(world);

            if (_isPrimaryPressed
                && _map.CurrentPlacementInputMode == PlacementInputMode.Continuous
                && !IsPointerBlocked())
            {
                _map.AddFurniture(world);
            }
        }

        private void OnButtonDown(PointerButtonDownPayload p)
        {
            if (p.Button != 0) return;
            if (IsPointerBlocked()) return;

            if (_authoring != null && _authoring.IsSelectMode)
            {
                _isPrimaryPressed = TryRaycast(_lastScreen, out var selectWorld)
                    && _map.SelectAuthoringObject(selectWorld);
                return;
            }

            if (_authoring != null && _authoring.IsEraseMode)
            {
                if (TryRaycast(_lastScreen, out var eraseWorld))
                    _map.RemoveAuthoringObject(eraseWorld);
                return;
            }

            if (_authoring != null && _authoring.IsDecorBlockerMode)
            {
                _isPrimaryPressed = true;
                if (TryRaycast(_lastScreen, out var blockerWorld))
                    _map.SetDecorBlocker(blockerWorld, _authoring.IsDecorBlockerPaintMode);
                return;
            }

            if (_map.IsPlayerRemovalMode)
            {
                _removalTapTracker.Begin(_lastScreen);
                return;
            }

            if (!_map.HasActivePlacement)
            {
                if (TryRaycast(_lastScreen, out var heldWorld))
                    BeginRemoveOptionHold(heldWorld);
                return;
            }

            _isPrimaryPressed = true;
            if (!TryRaycast(_lastScreen, out var world)) return;

            _map.UpdatePreview(world);
            if (_map.CurrentPlacementInputMode == PlacementInputMode.Continuous)
                _map.AddFurniture(world);
        }

        private void OnButtonUp(PointerButtonUpPayload p)
        {
            if (p.Button != 0) return;

            if (_input.IsGameplayInputBlocked)
            {
                _isPrimaryPressed = false;
                _removalTapTracker.Cancel();
                CancelRemoveOptionHold(true);
                return;
            }

            if (_map.IsPlayerRemovalMode)
            {
                bool isRemovalTap = _removalTapTracker.Complete(_lastScreen);
                if (isRemovalTap && !IsPointerBlocked()
                    && TryRaycast(_lastScreen, out var removeWorld))
                {
                    _map.RemovePlayerObject(removeWorld);
                    // Contextual removal is a one-shot action. This also lets the
                    // player leave the mode after tapping an empty/invalid cell.
                    _map.SetPlayerRemovalMode(false);
                }
                return;
            }

            if (_isRemoveOptionHoldTracking)
            {
                CancelRemoveOptionHold(!_didShowRemoveOption);
                return;
            }

            bool wasPressed = _isPrimaryPressed;
            _isPrimaryPressed = false;

            if (_authoring != null && _authoring.IsDecorBlockerMode) return;

            if (!wasPressed || !_map.HasActivePlacement) return;
            if (_map.CurrentPlacementInputMode != PlacementInputMode.Single) return;
            if (IsPointerBlocked()) return;
            if (TryRaycast(_lastScreen, out var world))
                _map.AddFurniture(world);
        }

        private void OnKey(KeyDownPayload p)
        {
            if (p.Key != KeyCode.Escape) return;
            CancelRemoveOptionHold(true);
            _map.StopPlacement();
        }

        private bool IsPointerBlocked()
        {
            return _input.IsGameplayInputBlocked
                || _input.IsPointerOverUI()
                || (_authoring != null && _authoring.IsPointerOverToolbar(_lastScreen));
        }
        #endregion

        #region Helpers
        private void BeginRemoveOptionHold(Vector3 world)
        {
            _removeOptionPublisher.Publish(new MapPlayerRemoveOptionPayload(false, default, default));
            _isRemoveOptionHoldTracking = true;
            _didShowRemoveOption = false;
            _removeOptionHoldStartedAt = Time.unscaledTime;
            _removeOptionHoldScreen = _lastScreen;
            _removeOptionHoldWorld = world;
        }

        private void CancelRemoveOptionHold(bool hideOption)
        {
            _isRemoveOptionHoldTracking = false;
            if (hideOption)
                _removeOptionPublisher?.Publish(new MapPlayerRemoveOptionPayload(false, default, default));
        }

        private Vector2 GetCellScreenCenter(Vector3 world)
        {
            Vector3Int cell = _map.WorldToCell(world);
            Vector3 cellCornerA = _map.CellToWorld(cell);
            Vector3 cellCornerB = _map.CellToWorld(cell + new Vector3Int(1, 0, 1));
            Vector3 cellCenter = (cellCornerA + cellCornerB) * 0.5f;
            return _camera.WorldToScreenPoint(cellCenter);
        }

        private bool TryRaycast(Vector2 screen, out Vector3 world)
        {
            var ray = _camera.ScreenPointToRay(screen);
            if (_useMathPlane)
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    world = ray.GetPoint(enter);
                    return true;
                }
                world = default;
                return false;
            }

            if (Physics.Raycast(ray, out var hit, _maxRayDistance, _placeLayer))
            {
                world = hit.point;
                return true;
            }
            world = default;
            return false;
        }
        #endregion
    }
}
