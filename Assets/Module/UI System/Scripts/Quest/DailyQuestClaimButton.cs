using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyOwn.ServiceHarness
{
    [RequireComponent(typeof(Button))]
    public sealed class DailyQuestClaimButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _visual;
        [SerializeField, Min(0.01f)] private float _hoverDuration = 0.12f;
        [SerializeField, Range(1f, 1.2f)] private float _hoverScale = 1.04f;
        [SerializeField] private Color _hoverTint =
            new Color(1f, 1f, 0.82f, 1f);

        private Vector3 _baseScale;
        private Color _baseColor = Color.white;
        private Tween _scaleTween;
        private Tween _colorTween;

        public Button Button => _button;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_visual == null) _visual = GetComponent<Image>();
            _baseScale = transform.localScale;
            if (_visual != null) _baseColor = _visual.color;
        }

        public void Configure(Button button, Image visual)
        {
            _button = button != null ? button : GetComponent<Button>();
            _visual = visual != null ? visual : GetComponent<Image>();
            _baseScale = transform.localScale;
            if (_visual != null) _baseColor = _visual.color;
        }

        public void SetInteractable(bool interactable)
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.interactable = interactable;
            if (!interactable) ResetVisuals(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.IsInteractable()) return;
            AnimateTo(_baseScale * _hoverScale, _hoverTint);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetVisuals(true);
        }

        private void OnDisable()
        {
            ResetVisuals(false);
        }

        private void AnimateTo(Vector3 scale, Color color)
        {
            _scaleTween?.Kill(false);
            _scaleTween = transform
                .DOScale(scale, _hoverDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_visual == null) return;
            _colorTween?.Kill(false);
            _colorTween = _visual
                .DOColor(color, _hoverDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ResetVisuals(bool animated)
        {
            _scaleTween?.Kill(false);
            _colorTween?.Kill(false);
            _scaleTween = null;
            _colorTween = null;

            if (!animated || !gameObject.activeInHierarchy)
            {
                transform.localScale = _baseScale;
                if (_visual != null) _visual.color = _baseColor;
                return;
            }

            AnimateTo(_baseScale, _baseColor);
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill(false);
            _colorTween?.Kill(false);
        }
    }
}
