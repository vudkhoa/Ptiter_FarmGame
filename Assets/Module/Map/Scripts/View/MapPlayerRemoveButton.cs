using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapPlayerRemoveButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IMapService _map;
        private MapAuthoringController _authoring;
        private RectTransform _rectTransform;
        private Vector3 _selectedWorldPosition;
        private bool _hasSelection;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(
            IMapService map,
            MapAuthoringController authoring,
            ISubscriber<MapPlacementStartedPayload> startSub,
            ISubscriber<MapPlacementStoppedPayload> stopSub,
            ISubscriber<MapPlayerRemovalModeChangedPayload> removalModeSub,
            ISubscriber<MapPlayerRemoveOptionPayload> removeOptionSub)
        {
            _map = map;
            _authoring = authoring;
            _button.onClick.AddListener(RemoveSelected);

            var bag = DisposableBag.CreateBuilder();
            startSub.Subscribe(_ => Hide()).AddTo(bag);
            stopSub.Subscribe(_ => Hide()).AddTo(bag);
            removalModeSub.Subscribe(_ => Hide()).AddTo(bag);
            removeOptionSub.Subscribe(OnRemoveOption).AddTo(bag);
            _subscriptions = bag.Build();

            Hide();
        }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _rectTransform = transform as RectTransform;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(RemoveSelected);
            _subscriptions?.Dispose();
        }

        private void RemoveSelected()
        {
            if (_map == null || !_hasSelection) return;

            Vector3 selectedWorldPosition = _selectedWorldPosition;
            Hide();
            _map.SetPlayerRemovalMode(true);
            _map.RemovePlayerObject(selectedWorldPosition);
            _map.SetPlayerRemovalMode(false);
        }

        private void OnRemoveOption(MapPlayerRemoveOptionPayload payload)
        {
            if (!payload.IsVisible || _map == null ||
                (_authoring != null && _authoring.IsAuthoringMode) ||
                _map.HasActivePlacement || _map.IsPlayerRemovalMode)
            {
                Hide();
                return;
            }

            _selectedWorldPosition = payload.WorldPosition;
            _hasSelection = true;
            gameObject.SetActive(true);
            PositionNear(payload.ScreenPosition);
        }

        private void PositionNear(Vector2 screenPosition)
        {
            if (_rectTransform == null) return;

            Vector2 scale = _rectTransform.lossyScale;
            Vector2 halfSize = Vector2.Scale(_rectTransform.rect.size, scale) * 0.5f;
            float verticalOffset = 24f * Mathf.Max(0.01f, scale.y);
            var position = new Vector2(
                Mathf.Clamp(screenPosition.x, halfSize.x, Screen.width - halfSize.x),
                Mathf.Clamp(screenPosition.y + halfSize.y + verticalOffset, halfSize.y, Screen.height - halfSize.y));
            _rectTransform.position = position;
        }

        private void Hide()
        {
            _hasSelection = false;
            gameObject.SetActive(false);
        }
    }
}
