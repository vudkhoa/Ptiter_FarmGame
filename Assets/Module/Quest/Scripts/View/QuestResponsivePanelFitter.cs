using UnityEngine;

namespace Core.Module.Quest.View
{
    /// <summary>
    /// Keeps the fixed-size Quest artwork inside its responsive viewport while
    /// preserving the original 1800 x 1200 design coordinate system.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class QuestResponsivePanelFitter : MonoBehaviour
    {
        private static readonly Vector2 DefaultDesignSize =
            new Vector2(1800f, 1200f);

        [SerializeField] private Vector2 _designSize =
            new Vector2(1800f, 1200f);
        [SerializeField, Min(0f)] private float _edgeMargin = 24f;
        [SerializeField] private bool _allowUpscale;

        private RectTransform _rectTransform;
        private RectTransform _viewport;
        private Vector2 _lastViewportSize = new Vector2(-1f, -1f);

        private void OnEnable()
        {
            Canvas.willRenderCanvases += RefreshIfNeeded;
            ApplyFit();
        }

        private void Start() => ApplyFit();

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= RefreshIfNeeded;
        }

        private void OnTransformParentChanged()
        {
            _viewport = null;
            _lastViewportSize = new Vector2(-1f, -1f);
            ApplyFit();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_designSize.x <= 0f || _designSize.y <= 0f)
                _designSize = DefaultDesignSize;

            ApplyFit();
        }
#endif

        public void ApplyFit()
        {
            ResolveReferences();
            if (_rectTransform == null || _viewport == null)
                return;

            Vector2 viewportSize = _viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f ||
                _designSize.x <= 0f || _designSize.y <= 0f)
                return;

            float availableWidth = Mathf.Max(
                1f, viewportSize.x - _edgeMargin * 2f);
            float availableHeight = Mathf.Max(
                1f, viewportSize.y - _edgeMargin * 2f);
            float fitScale = Mathf.Min(
                availableWidth / _designSize.x,
                availableHeight / _designSize.y);

            if (!_allowUpscale)
                fitScale = Mathf.Min(1f, fitScale);

            _rectTransform.anchorMin = Vector2.one * 0.5f;
            _rectTransform.anchorMax = Vector2.one * 0.5f;
            _rectTransform.pivot = Vector2.one * 0.5f;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.sizeDelta = _designSize;
            _rectTransform.localScale = Vector3.one * fitScale;
            _lastViewportSize = viewportSize;
        }

        private void RefreshIfNeeded()
        {
            ResolveReferences();
            if (_viewport == null)
                return;

            Vector2 viewportSize = _viewport.rect.size;
            if ((viewportSize - _lastViewportSize).sqrMagnitude > 0.01f)
                ApplyFit();
        }

        private void ResolveReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_viewport == null)
                _viewport = transform.parent as RectTransform;
        }
    }
}
