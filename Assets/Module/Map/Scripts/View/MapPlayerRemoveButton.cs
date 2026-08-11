using System;
using DG.Tweening;
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
        [SerializeField] private Camera _worldCamera;

        [Header("Position")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 24f);

        [Header("Show Animation")]
        [SerializeField, Min(0.01f)] private float _showDuration = 0.18f;
        [SerializeField, Range(0.1f, 1f)] private float _showStartScale = 0.72f;

        private IMapService _map;
        private MapAuthoringController _authoring;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector3 _restingScale;
        private Vector3 _selectedWorldPosition;
        private Vector3 _selectedAnchorWorldPosition;
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
            if (_worldCamera == null) _worldCamera = Camera.main;
            _rectTransform = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _restingScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            // Update after camera LateUpdate so smooth zoom does not leave the
            // contextual button at the previous frame's screen position.
            Canvas.willRenderCanvases += RefreshPosition;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= RefreshPosition;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
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
            _selectedAnchorWorldPosition = GetCellCenter(payload.WorldPosition);
            _hasSelection = true;
            gameObject.SetActive(true);
            PositionNear(payload.ScreenPosition);
            PlayShowAnimation();
        }

        private Vector3 GetCellCenter(Vector3 worldPosition)
        {
            Vector3Int cell = _map.WorldToCell(worldPosition);
            Vector3 cornerA = _map.CellToWorld(cell);
            Vector3 cornerB = _map.CellToWorld(cell + new Vector3Int(1, 0, 1));
            return (cornerA + cornerB) * 0.5f;
        }

        private void RefreshPosition()
        {
            if (!_hasSelection || _rectTransform == null) return;
            if (_worldCamera == null) _worldCamera = Camera.main;
            if (_worldCamera == null) return;

            Vector3 screenPosition = _worldCamera.WorldToScreenPoint(_selectedAnchorWorldPosition);
            if (screenPosition.z > 0f)
                PositionNear(screenPosition);
        }

        private void PositionNear(Vector2 screenPosition)
        {
            if (_rectTransform == null) return;

            Vector3 parentScale = _rectTransform.parent != null
                ? _rectTransform.parent.lossyScale
                : Vector3.one;
            var scale = new Vector2(
                Mathf.Abs(parentScale.x * _restingScale.x),
                Mathf.Abs(parentScale.y * _restingScale.y));
            Vector2 halfSize = Vector2.Scale(_rectTransform.rect.size, scale) * 0.5f;
            Vector2 scaledOffset = Vector2.Scale(_screenOffset, scale);
            var position = new Vector2(
                Mathf.Clamp(screenPosition.x + scaledOffset.x, halfSize.x, Screen.width - halfSize.x),
                Mathf.Clamp(screenPosition.y + halfSize.y + scaledOffset.y, halfSize.y, Screen.height - halfSize.y));
            _rectTransform.position = position;
        }

        private void Hide()
        {
            _hasSelection = false;
            DOTween.Kill(this);
            if (_rectTransform != null) _rectTransform.localScale = _restingScale;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void PlayShowAnimation()
        {
            if (_rectTransform == null || _canvasGroup == null) return;

            DOTween.Kill(this);
            _rectTransform.localScale = _restingScale * _showStartScale;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            DOTween.Sequence()
                .Join(_rectTransform.DOScale(_restingScale, _showDuration)
                    .SetEase(Ease.OutBack))
                .Join(DOTween.To(
                        () => _canvasGroup.alpha,
                        value => _canvasGroup.alpha = value,
                        1f,
                        _showDuration * 0.7f)
                    .SetEase(Ease.OutQuad))
                .SetUpdate(true)
                .SetTarget(this);
        }
    }
}
