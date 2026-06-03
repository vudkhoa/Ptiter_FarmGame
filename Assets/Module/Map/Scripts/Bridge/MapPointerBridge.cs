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

        private IMapService _map;
        private IInputService _input;
        private Vector2 _lastScreen;
        private IDisposable _subscriptions;

        #region DI - Constructor
        [Inject]
        public void Construct(
           IMapService map,
           IInputService input,
           ISubscriber<PointerScreenPayload> screenSub,
           ISubscriber<PointerButtonDownPayload> btnSub,
           ISubscriber<KeyDownPayload> keySub)
        {
            _map = map;
            _input = input;

            var bag = DisposableBag.CreateBuilder();
            screenSub.Subscribe(OnScreen).AddTo(bag);
            btnSub.Subscribe(OnButton).AddTo(bag);
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
            if (!_map.HasActivePlacement) return;
            if (TryRaycast(_lastScreen, out var world))
                _map.UpdatePreview(world);
        }

        private void OnButton(PointerButtonDownPayload p)
        {
            if (p.Button != 0 || !_map.HasActivePlacement) return;
            if (_input.IsPointerOverUI()) return;
            if (TryRaycast(_lastScreen, out var world))
                _map.AddFurniture(world);
        }

        private void OnKey(KeyDownPayload p)
        {
            if (p.Key == KeyCode.Escape) _map.StopPlacement();
        }
        #endregion

        #region Helpers
        private bool TryRaycast(Vector2 screen, out Vector3 world)
        {
            var ray = _camera.ScreenPointToRay(screen);
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
