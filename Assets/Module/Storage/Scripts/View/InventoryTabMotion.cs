using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Module.Storage.View
{
    [DisallowMultipleComponent]
    public sealed class InventoryTabMotion :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private RectTransform _rectTransform;
        private Tween _moveTween;
        private float _expandedX;
        private float _collapsedX;
        private float _duration;
        private Ease _ease;
        private bool _isActive;
        private bool _isHovered;
        private bool _initialized;

        public void Initialize(
            float expandedX,
            float collapsedX,
            float duration,
            Ease ease)
        {
            _rectTransform = transform as RectTransform;
            _expandedX = expandedX;
            _collapsedX = collapsedX;
            _duration = duration;
            _ease = ease;
            _initialized = _rectTransform != null;
        }

        public void SetActive(bool isActive, bool animate)
        {
            _isActive = isActive;
            RefreshPosition(animate);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshPosition(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshPosition(true);
        }

        private void RefreshPosition(bool animate)
        {
            if (!_initialized) return;

            float targetX = _isActive || _isHovered
                ? _expandedX
                : _collapsedX;

            _moveTween?.Kill();
            if (!animate || _duration <= 0f)
            {
                Vector2 position = _rectTransform.anchoredPosition;
                position.x = targetX;
                _rectTransform.anchoredPosition = position;
                return;
            }

            _moveTween = DOTween
                .To(
                    () => _rectTransform.anchoredPosition.x,
                    x =>
                    {
                        Vector2 position = _rectTransform.anchoredPosition;
                        position.x = x;
                        _rectTransform.anchoredPosition = position;
                    },
                    targetX,
                    _duration)
                .SetEase(_ease)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void OnDisable()
        {
            _isHovered = false;
            _moveTween?.Kill();
            _moveTween = null;
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }
    }
}
