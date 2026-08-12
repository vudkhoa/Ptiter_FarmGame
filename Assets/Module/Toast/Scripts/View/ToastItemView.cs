using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Toast
{
    /// One pooled bubble. Owns its own fade/scale/hold sequence plus a separate position tween,
    /// so the stack can re-flow it at any moment without cutting the animation that is running.
    [DisallowMultipleComponent]
    public sealed class ToastItemView : MonoBehaviour
    {
        /// Canvas units. Past this the bubble stops growing sideways and wraps instead.
        private const float MaxWidth = 1100f;
        private const float ReflowDuration = 0.18f;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private ContentSizeFitter _fitter;

        private ToastConfigSO _config;
        private Action<ToastItemView> _onFinished;
        private Sequence _sequence;
        private Tween _positionTween;
        private float _targetY;

        #region Properties
        public string Message { get; private set; }

        public float Height => _rect != null ? _rect.rect.height : 0f;
        #endregion

        #region Unity Lifecycle
        private void Awake() => CacheParts();

        private void OnDestroy() => KillTweens();
        #endregion

        #region Public API
        public void Configure(ToastConfigSO config, Action<ToastItemView> onFinished)
        {
            CacheParts();
            _config = config;
            _onFinished = onFinished;
            gameObject.SetActive(false);
        }

        /// Places the bubble at <paramref name="startY"/> and plays it through to release.
        public void Play(in ToastRequest request, float startY)
        {
            CacheParts();
            if (_config == null || _rect == null || _group == null) return;

            KillTweens();

            Message = request.Message;
            gameObject.SetActive(true);

            ApplyContent(request);
            ResizeToContent();

            _targetY = startY;
            _rect.anchoredPosition = new Vector2(0f, startY - _config.riseDistance);
            _rect.localScale = Vector3.one * _config.popScale;
            _group.alpha = 0f;

            BuildSequence(_config.ResolveDuration(request.Duration));
            _positionTween = _rect
                .DOAnchorPosY(_targetY, _config.fadeInDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// Restarts the hold timer for a repeat of the same message, without re-entering.
        public void Restart(float duration)
        {
            if (_config == null || _rect == null || _group == null) return;
            if (!gameObject.activeSelf) return;

            _sequence?.Kill(false);
            _group.alpha = 1f;
            _rect.localScale = Vector3.one;
            BuildSequence(_config.ResolveDuration(duration), skipEntry: true);
        }

        /// Slides to a new slot after the toast above or below it left the stack.
        public void MoveTo(float y)
        {
            if (_rect == null || Mathf.Approximately(_targetY, y)) return;

            _targetY = y;
            _positionTween?.Kill(false);
            _positionTween = _rect
                .DOAnchorPosY(y, ReflowDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void HideImmediate()
        {
            KillTweens();
            Message = null;
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        private void CacheParts()
        {
            if (_rect == null) _rect = transform as RectTransform;
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_background == null) _background = GetComponent<Image>();
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
            if (_fitter == null) _fitter = GetComponent<ContentSizeFitter>();
        }

        private void ApplyContent(in ToastRequest request)
        {
            if (_label != null)
            {
                _label.text = request.Message;
                if (_config.overrideBackgroundColor) _label.color = _config.textColor;
            }

            if (_background != null && _config.overrideBackgroundColor)
                _background.color = _config.ColorFor(request.Style);
        }

        /// Grows with the text, then clamps: past MaxWidth the fitter is switched off horizontally
        /// so the label wraps. Rebuilt immediately because the stack needs a real height this frame.
        private void ResizeToContent()
        {
            if (_fitter == null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
                return;
            }

            _fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);

            if (_rect.rect.width <= MaxWidth) return;

            _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            _rect.sizeDelta = new Vector2(MaxWidth, _rect.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        }

        private void BuildSequence(float holdDuration, bool skipEntry = false)
        {
            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (!skipEntry)
            {
                _sequence.Append(_group.DOFade(1f, _config.fadeInDuration));
                _sequence.Join(_rect.DOScale(1f, _config.fadeInDuration).SetEase(Ease.OutBack));
            }

            _sequence.AppendInterval(holdDuration);
            _sequence.Append(_group.DOFade(0f, _config.fadeOutDuration));
            _sequence.OnComplete(Release);
        }

        private void Release()
        {
            _sequence = null;
            Action<ToastItemView> callback = _onFinished;
            HideImmediate();
            callback?.Invoke(this);
        }

        private void KillTweens()
        {
            _sequence?.Kill(false);
            _sequence = null;
            _positionTween?.Kill(false);
            _positionTween = null;
        }
        #endregion
    }
}
