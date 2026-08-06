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

        private IMapService _map;
        private IInputService _input;
        private MapAuthoringController _authoring;
        private Vector2 _lastScreen;
        private bool _isPrimaryPressed;
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
           ISubscriber<KeyDownPayload> keySub)
        {
            _map = map;
            _input = input;
            _authoring = authoring;

            var bag = DisposableBag.CreateBuilder();
            screenSub.Subscribe(OnScreen).AddTo(bag);
            btnDownSub.Subscribe(OnButtonDown).AddTo(bag);
            btnUpSub.Subscribe(OnButtonUp).AddTo(bag);
            keySub.Subscribe(OnKey).AddTo(bag);
            _subscriptions = bag.Build();
        }
        #endregion

        #region Unity LifeCycle
        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
        #endregion

        #region Sub Logic
        private void OnScreen(PointerScreenPayload p)
        {
            _lastScreen = p.ScreenPosition;

            if (_authoring != null && _authoring.IsSelectMode && _isPrimaryPressed)
            {
                if (!IsPointerBlocked() && TryRaycast(_lastScreen, out var moveWorld))
                    _map.MoveSelectedAuthoringObject(moveWorld);
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

            if (_map.IsPlayerRemovalMode)
            {
                if (TryRaycast(_lastScreen, out var removeWorld))
                    _map.RemovePlayerObject(removeWorld);
                return;
            }

            if (!_map.HasActivePlacement) return;

            _isPrimaryPressed = true;
            if (!TryRaycast(_lastScreen, out var world)) return;

            _map.UpdatePreview(world);
            if (_map.CurrentPlacementInputMode == PlacementInputMode.Continuous)
                _map.AddFurniture(world);
        }

        private void OnButtonUp(PointerButtonUpPayload p)
        {
            if (p.Button != 0) return;

            bool wasPressed = _isPrimaryPressed;
            _isPrimaryPressed = false;

            if (!wasPressed || !_map.HasActivePlacement) return;
            if (_map.CurrentPlacementInputMode != PlacementInputMode.Single) return;
            if (IsPointerBlocked()) return;
            if (TryRaycast(_lastScreen, out var world))
                _map.AddFurniture(world);
        }

        private void OnKey(KeyDownPayload p)
        {
            if (p.Key == KeyCode.Escape) _map.StopPlacement();
        }

        private bool IsPointerBlocked()
        {
            return _input.IsPointerOverUI()
                || (_authoring != null && _authoring.IsPointerOverToolbar(_lastScreen));
        }
        #endregion

        #region Helpers
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
