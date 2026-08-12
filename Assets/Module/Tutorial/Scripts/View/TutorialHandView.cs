using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Draws the pointing hand, the focus ring, the hint bubble and the input mask for one step.
    /// Re-resolves its anchor every LateUpdate so the hand tracks a panning camera and a farm
    /// slot that spawns a frame after the step starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialHandView : MonoBehaviour, ITutorialView
    {
        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;

        [Header("Hand")]
        [Tooltip("Positioner: follows the anchor. Never animated, so tweens cannot fight tracking.")]
        [SerializeField] private RectTransform _handRoot;

        [Tooltip(
            "Animated child. Owns every tween. Its PIVOT must sit on the fingertip: that is what " +
            "makes offset (0,0) point exactly at the anchor and keeps the fingertip planted while " +
            "the tap scales.")]
        [SerializeField] private RectTransform _hand;
        [SerializeField] private CanvasGroup _handGroup;

        [Header("Focus")]
        [SerializeField] private RectTransform _focusRing;
        [SerializeField] private Image _focusImage;

        [Header("Mask")]
        [Tooltip("Full-screen black at low opacity. Swallows every tap that is not the highlight.")]
        [SerializeField] private Image _dimmer;

        [Tooltip(
            "Holder for the highlighted widget's clone. Must start INACTIVE: cloning into an " +
            "inactive parent is what stops the copy's own scripts from ever running Awake/OnEnable.")]
        [SerializeField] private RectTransform _highlightRoot;

        [Header("Hint")]
        [SerializeField] private RectTransform _hintRoot;
        [SerializeField] private TMP_Text _hintLabel;

        /// <summary>Canvas units kept between the hint bubble and the screen edge.</summary>
        private const float HintScreenMargin = 24f;

        private TutorialStepSO _step;
        private Sequence _motion;
        private Camera _worldCamera;
        private float _elapsed;
        private bool _visualsArmed;
        private bool _warnedUnmaskableAnchor;

        private Sprite _defaultFocusSprite;
        private readonly List<HiddenAnchor> _hiddenAnchors = new List<HiddenAnchor>();

        private RectTransform _highlightSource;
        private RectTransform _highlightClone;
        private RectTransform _clickCatcher;
        private Vector2 _highlightSourceSize;
        private TutorialDimMask _dimMask;

        public bool IsShowing => _step != null;

        /// <summary>Remembers what a hidden anchor looked like so the step can put it back.</summary>
        private readonly struct HiddenAnchor
        {
            public readonly CanvasGroup Group;
            public readonly float Alpha;
            public readonly bool BlocksRaycasts;
            public readonly bool Added;

            public HiddenAnchor(CanvasGroup group, float alpha, bool blocksRaycasts, bool added)
            {
                Group = group;
                Alpha = alpha;
                BlocksRaycasts = blocksRaycasts;
                Added = added;
            }
        }

        #region ITutorialView
        public void ShowStep(TutorialStepSO step)
        {
            if (step == null)
            {
                Hide();
                return;
            }

            _step = step;
            _elapsed = 0f;
            _visualsArmed = false;
            _warnedUnmaskableAnchor = false;

            gameObject.SetActive(true);
            RestoreHiddenAnchors();
            ApplyHiddenAnchors(step);
            ApplyFocusSprite(step);
            ApplyStaticHandSettings(step.hand);
            SetHintText(step.hintText);
            SetVisualsVisible(false);
            BuildMotion();
        }

        public void Hide()
        {
            _step = null;
            _visualsArmed = false;
            KillMotion();
            ClearHighlight();
            RestoreHiddenAnchors();
            SetVisualsVisible(false);
            gameObject.SetActive(false);
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (_focusImage == null && _focusRing != null)
                _focusImage = _focusRing.GetComponent<Image>();
            if (_focusImage != null) _defaultFocusSprite = _focusImage.sprite;

            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvasRect == null && _canvas != null)
                _canvasRect = _canvas.transform as RectTransform;

            // Only the mask subclass can leave a hole for a world cell. A container authored
            // before it existed still holds a plain Image, which ApplyMask reports on.
            _dimMask = _dimmer as TutorialDimMask;

            // A container authored before the dim + clone rework still works, it just cannot mask.
            // Say so rather than leaving someone wondering where the dim went.
            if (_dimmer == null || _highlightRoot == null)
            {
                Debug.LogWarning(
                    "[TutorialHandView] Dimmer or Highlight Root is unwired - steps with " +
                    "blockInputOutsideFocus will not mask. Run Tools/Tutorial/Rebuild Tutorial Content.",
                    this);
            }

            // Without these three there is nothing to place or animate, and LateUpdate would
            // throw once per frame for the whole step. Fail loudly once instead.
            if (_canvasRect != null && _handRoot != null && _hand != null) return;

            Debug.LogError(
                "[TutorialHandView] Canvas rect, hand root or hand image is unwired. " +
                "Run Tools/Tutorial/Rebuild Tutorial Content.", this);
            enabled = false;
        }

        private void OnDisable()
        {
            ClearHighlight();
            RestoreHiddenAnchors();
            KillMotion();
        }

        private void LateUpdate()
        {
            if (_step == null) return;

            // Unscaled: a tutorial must still animate while a paused popup owns the time scale.
            // Fully qualified because Core.Module.Time would otherwise shadow UnityEngine.Time.
            _elapsed += UnityEngine.Time.unscaledDeltaTime;
            if (_elapsed < _step.startDelay) return;

            if (!TryResolveAnchor(
                    _step.hand, out Vector2 canvasPoint, out Vector2 anchorSize, out RectTransform uiTarget))
            {
                // The target is not on screen yet (scene still loading, slot not spawned).
                // Showing a hand over empty ground is worse than showing nothing.
                if (_visualsArmed) SetVisualsVisible(false);
                _visualsArmed = false;
                ClearHighlight(true);
                return;
            }

            if (!_visualsArmed)
            {
                _visualsArmed = true;
                SetVisualsVisible(true);
            }

            _handRoot.anchoredPosition = canvasPoint + _step.hand.offset;
            LayoutFocus(canvasPoint, anchorSize);
            LayoutHint(canvasPoint);
            ApplyMask(uiTarget, canvasPoint, anchorSize);
        }
        #endregion

        #region Layout
        /// <summary>
        /// Resolves the step anchor into this canvas' local space, plus the size the focus ring
        /// should hug. <paramref name="uiTarget"/> is the resolved widget when the anchor is UI,
        /// null otherwise - only a UI widget can be cloned onto the dim.
        /// Returns false when nothing under the anchor id is currently on screen.
        /// </summary>
        private bool TryResolveAnchor(
            TutorialHandConfig hand,
            out Vector2 canvasPoint,
            out Vector2 anchorSize,
            out RectTransform uiTarget)
        {
            canvasPoint = Vector2.zero;
            anchorSize = _step.focusFallbackSize;
            uiTarget = null;

            if (_canvasRect == null) return false;

            Vector2 screenPoint;

            switch (hand.anchorMode)
            {
                case TutorialAnchorMode.ScreenPercent:
                    screenPoint = new Vector2(
                        hand.screenPercent.x * Screen.width,
                        hand.screenPercent.y * Screen.height);
                    break;

                case TutorialAnchorMode.WorldPoint:
                    if (!TryProjectWorld(hand.worldPosition, out screenPoint)) return false;
                    break;

                default:
                    if (!TutorialAnchorRegistry.TryResolve(
                            hand.anchorId, out Transform target, out Vector3 worldPoint))
                        return false;

                    if (target is RectTransform rect)
                    {
                        if (!TryGetUiScreenRect(rect, out Rect screenRect)) return false;
                        screenPoint = screenRect.center;
                        anchorSize = screenRect.size / CanvasScale;
                        uiTarget = rect;
                    }
                    else
                    {
                        if (!TryProjectWorld(worldPoint, out screenPoint)) return false;

                        // Size the highlight from the target's real world footprint when the
                        // bridge pinned one. A fixed pixel size stops matching the moment the
                        // camera zooms, which is what made the cell marker look wrong.
                        Vector3 halfExtent = TutorialAnchorRegistry.GetWorldHalfExtent(hand.anchorId);
                        if (halfExtent != Vector3.zero &&
                            TryProjectFootprint(worldPoint, halfExtent, out Vector2 footprint))
                            anchorSize = footprint / CanvasScale;
                    }
                    break;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, CanvasCamera, out canvasPoint);
        }

        private bool TryProjectWorld(Vector3 worldPoint, out Vector2 screenPoint)
        {
            screenPoint = Vector2.zero;

            Camera worldCamera = ResolveWorldCamera();
            if (worldCamera == null) return false;

            Vector3 projected = worldCamera.WorldToScreenPoint(worldPoint);
            // Behind the camera: WorldToScreenPoint mirrors the point instead of failing.
            if (projected.z <= 0f) return false;

            screenPoint = projected;
            return true;
        }

        /// <summary>
        /// Screen-space size of a world-space box, from the bounding box of its four ground
        /// corners. Isometric cells are diamonds, so the axis-aligned bound is exactly the
        /// rectangle the diamond sprite has to fill.
        /// </summary>
        private bool TryProjectFootprint(Vector3 worldCenter, Vector3 halfExtent, out Vector2 size)
        {
            size = Vector2.zero;

            Camera worldCamera = ResolveWorldCamera();
            if (worldCamera == null) return false;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = worldCenter + new Vector3(
                    (i & 1) == 0 ? -halfExtent.x : halfExtent.x,
                    0f,
                    (i & 2) == 0 ? -halfExtent.z : halfExtent.z);

                Vector3 projected = worldCamera.WorldToScreenPoint(corner);
                if (projected.z <= 0f) return false;

                Vector2 point = projected;
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            size = max - min;
            return size.x > 0f && size.y > 0f;
        }

        private static bool TryGetUiScreenRect(RectTransform rect, out Rect screenRect)
        {
            screenRect = default;
            if (!rect.gameObject.activeInHierarchy) return false;

            Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
            if (sourceCanvas == null) return false;

            Camera sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : sourceCanvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            screenRect = new Rect(min, max - min);
            return screenRect.width > 0f && screenRect.height > 0f;
        }

        private void LayoutFocus(Vector2 center, Vector2 anchorSize)
        {
            if (_focusRing == null) return;

            _focusRing.gameObject.SetActive(_step.showFocusRing);
            if (!_step.showFocusRing) return;

            _focusRing.anchoredPosition = center + _step.focusOffset;
            _focusRing.sizeDelta = anchorSize + _step.focusPadding;
        }

        /// <summary>How far the hand artwork reaches below its fingertip pivot, in canvas units.</summary>
        private float HandDropHeight()
        {
            if (_hand == null || _handRoot == null) return 0f;
            return _hand.rect.height * _hand.pivot.y * Mathf.Abs(_handRoot.localScale.y);
        }

        private void LayoutHint(Vector2 center)
        {
            if (_hintRoot == null) return;
            if (!_hintRoot.gameObject.activeSelf) return;

            // Measured from the BOTTOM OF THE HAND, not from the anchor: the artwork hangs about
            // 147 units below its fingertip pivot, so an anchor-relative offset would have to
            // bake that in by hand and drift the moment the hand is scaled or re-pivoted.
            Vector2 target = center + _step.hand.offset
                           + new Vector2(0f, -HandDropHeight())
                           + _step.hintOffset;

            // A target near a screen edge would push the bubble half off-screen and cut the text.
            // Slide it back in rather than letting the player read half a sentence.
            Vector2 canvasHalf = _canvasRect.rect.size * 0.5f;
            Vector2 hintHalf = _hintRoot.rect.size * 0.5f;
            float limitX = Mathf.Max(0f, canvasHalf.x - hintHalf.x - HintScreenMargin);
            float limitY = Mathf.Max(0f, canvasHalf.y - hintHalf.y - HintScreenMargin);

            _hintRoot.anchoredPosition = new Vector2(
                Mathf.Clamp(target.x, -limitX, limitX),
                Mathf.Clamp(target.y, -limitY, limitY));
        }
        #endregion

        #region Mask
        /// <summary>
        /// Dims everything, then opens exactly one way through it. A UI widget gets a dead copy
        /// drawn on top of the dim with an invisible catcher relaying taps back to the real one -
        /// a copy matches the silhouette exactly, which a rectangular hole never does. A world
        /// cell has no widget to copy, so the dim keeps swallowing every tap EXCEPT the cell's own
        /// rect, which it reports as a miss so the map raycast underneath still sees it.
        /// </summary>
        private void ApplyMask(RectTransform uiTarget, Vector2 canvasPoint, Vector2 anchorSize)
        {
            if (!_step.dimBackground)
            {
                SetDimmer(false, false);
                ClearHighlight(true);
                return;
            }

            bool wantsGate = _step.blockInputOutsideFocus;

            if (uiTarget == null)
            {
                // Sized off the anchor, not the ring: focusOffset can park the ring away from the
                // cell for artwork reasons, but the tappable patch has to stay on the real cell.
                bool canGate = wantsGate && _dimMask != null;
                if (canGate)
                    _dimMask.SetHole(_canvasRect, canvasPoint, anchorSize + _step.focusPadding);
                else
                    WarnUnmaskableAnchor(wantsGate);

                SetDimmer(true, canGate);
                ClearHighlight(true);
                return;
            }

            // The widget is reachable through its clone, so the dim must stay solid here.
            if (_dimMask != null) _dimMask.ClearHole();
            SetDimmer(true, wantsGate);

            if (!wantsGate)
            {
                ClearHighlight(true);
                return;
            }

            if (_highlightSource != uiTarget) BuildHighlight(uiTarget);
            if (_highlightClone == null) return;

            // Position and fit run every frame so the copy follows a moving or rescaling original.
            _highlightRoot.anchoredPosition = canvasPoint;
            _highlightRoot.localScale = ResolveFitScale(anchorSize);
        }

        /// <summary>
        /// The clone keeps the original's own rect size so its children stay laid out; the holder
        /// scales that rect up or down to whatever the original currently measures on screen.
        /// </summary>
        private Vector3 ResolveFitScale(Vector2 anchorSize)
        {
            float x = _highlightSourceSize.x > 0.01f ? anchorSize.x / _highlightSourceSize.x : 1f;
            float y = _highlightSourceSize.y > 0.01f ? anchorSize.y / _highlightSourceSize.y : 1f;
            return new Vector3(x, y, 1f);
        }

        private void BuildHighlight(RectTransform source)
        {
            ClearHighlight();
            if (_highlightRoot == null || source == null) return;

            _highlightSource = source;
            _highlightSourceSize = source.rect.size;

            // Instantiate into the INACTIVE holder: the copy's Awake/OnEnable never run, so a
            // cloned TutorialAnchor cannot register itself and shadow the real one in the registry.
            GameObject clone = Instantiate(source.gameObject, _highlightRoot);
            clone.name = "Fake Target";
            SanitizeClone(clone);

            _highlightClone = clone.transform as RectTransform;
            if (_highlightClone != null)
            {
                _highlightClone.anchorMin = _highlightClone.anchorMax = new Vector2(0.5f, 0.5f);
                _highlightClone.pivot = new Vector2(0.5f, 0.5f);
                _highlightClone.sizeDelta = _highlightSourceSize;
                _highlightClone.anchoredPosition = Vector2.zero;
                _highlightClone.localRotation = Quaternion.identity;
                _highlightClone.localScale = Vector3.one;
            }

            _clickCatcher = CreateClickCatcher(source.gameObject);
            _highlightRoot.gameObject.SetActive(true);
            BuildMotion();
        }

        private RectTransform CreateClickCatcher(GameObject realTarget)
        {
            GameObject catcher = new GameObject(
                "Click Catcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(TutorialClickForwarder));

            RectTransform rect = (RectTransform)catcher.transform;
            rect.SetParent(_highlightRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = _highlightSourceSize;

            Image image = catcher.GetComponent<Image>();
            // Fully transparent would still raycast, but a hair of alpha survives any future
            // cullTransparentMesh pass on the canvas.
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;

            catcher.GetComponent<TutorialClickForwarder>().Bind(realTarget);
            return rect;
        }

        /// <summary>
        /// Strips everything that makes the copy behave like the original. Left alone: Graphics,
        /// layout components and CanvasRenderer, which are all that is needed to look identical.
        /// </summary>
        private static void SanitizeClone(GameObject clone)
        {
            MonoBehaviour[] behaviours = clone.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                if (behaviour is Graphic) continue;
                if (behaviour is ILayoutElement || behaviour is ILayoutController) continue;

                Destroy(behaviour);
            }

            Graphic[] graphics = clone.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;

            // A nested Canvas that overrides sorting would punch the copy out of our layering.
            // Neutralised rather than destroyed: GraphicRaycaster declares RequireComponent(Canvas).
            Canvas[] canvases = clone.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++) canvases[i].overrideSorting = false;
        }

        /// <summary>
        /// <paramref name="resyncMotion"/> rebuilds the shared beat: the clone is one of its
        /// targets, so losing it mid-step would leave a sequence tweening a destroyed transform.
        /// </summary>
        private void ClearHighlight(bool resyncMotion = false)
        {
            bool hadClone = _highlightClone != null;

            if (_highlightClone != null) Destroy(_highlightClone.gameObject);
            if (_clickCatcher != null) Destroy(_clickCatcher.gameObject);

            _highlightClone = null;
            _clickCatcher = null;
            _highlightSource = null;

            if (_highlightRoot != null)
            {
                _highlightRoot.localScale = Vector3.one;
                _highlightRoot.gameObject.SetActive(false);
            }

            if (resyncMotion && hadClone && _step != null) BuildMotion();
        }

        private void WarnUnmaskableAnchor(bool wantsGate)
        {
            if (!wantsGate || _warnedUnmaskableAnchor) return;

            _warnedUnmaskableAnchor = true;
            Debug.LogWarning(
                $"[TutorialHandView] Step '{_step.stepId}' gates a world anchor but the dim is a " +
                "plain Image, which cannot punch the cell out. Run Tools/Tutorial/Rebuild " +
                "Tutorial Content to swap it for TutorialDimMask.", this);
        }

        private void SetDimmer(bool visible, bool blocksInput)
        {
            if (_dimmer == null) return;

            bool gating = visible && blocksInput;
            if (_dimmer.gameObject.activeSelf != visible) _dimmer.gameObject.SetActive(visible);
            _dimmer.raycastTarget = gating;

            // A hole left over from the previous step would keep a dead patch of screen tappable.
            if (!gating && _dimMask != null) _dimMask.ClearHole();
        }
        #endregion

        #region Step decoration
        private void ApplyFocusSprite(TutorialStepSO step)
        {
            if (_focusImage == null) return;
            _focusImage.sprite = step.focusSprite != null ? step.focusSprite : _defaultFocusSprite;
        }

        /// <summary>
        /// Fades out anything the step declares as in the way, e.g. the object picker sitting on
        /// top of the plot the player is being told to tap. Uses a CanvasGroup rather than
        /// SetActive so a UIManager window is never told it closed behind its own back.
        /// </summary>
        private void ApplyHiddenAnchors(TutorialStepSO step)
        {
            if (step.hiddenAnchorIds == null) return;

            for (int i = 0; i < step.hiddenAnchorIds.Count; i++)
            {
                if (!TutorialAnchorRegistry.TryResolve(step.hiddenAnchorIds[i], out Transform target, out _))
                    continue;
                if (target == null) continue;

                CanvasGroup group = target.GetComponent<CanvasGroup>();
                bool added = group == null;
                if (added) group = target.gameObject.AddComponent<CanvasGroup>();

                _hiddenAnchors.Add(new HiddenAnchor(group, group.alpha, group.blocksRaycasts, added));
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        private void RestoreHiddenAnchors()
        {
            for (int i = 0; i < _hiddenAnchors.Count; i++)
            {
                HiddenAnchor hidden = _hiddenAnchors[i];
                if (hidden.Group == null) continue;

                hidden.Group.alpha = hidden.Alpha;
                hidden.Group.blocksRaycasts = hidden.BlocksRaycasts;
            }

            _hiddenAnchors.Clear();
        }
        #endregion

        #region Hand presentation
        private void ApplyStaticHandSettings(TutorialHandConfig hand)
        {
            if (_handRoot == null) return;

            _handRoot.localRotation = Quaternion.Euler(0f, 0f, hand.rotation);

            float scaleX = hand.flipX ? -hand.scale : hand.scale;
            _handRoot.localScale = new Vector3(scaleX, hand.scale, 1f);
        }

        private void SetHintText(string text)
        {
            if (_hintRoot == null) return;

            bool hasText = !string.IsNullOrWhiteSpace(text);
            _hintRoot.gameObject.SetActive(hasText);
            if (hasText && _hintLabel != null) _hintLabel.text = text;
        }

        private void SetVisualsVisible(bool visible)
        {
            if (_handRoot != null) _handRoot.gameObject.SetActive(visible);

            if (!visible)
            {
                if (_focusRing != null) _focusRing.gameObject.SetActive(false);
                if (_hintRoot != null) _hintRoot.gameObject.SetActive(false);
                SetDimmer(false, false);
                return;
            }

            if (_hintRoot != null && _step != null)
                _hintRoot.gameObject.SetActive(!string.IsNullOrWhiteSpace(_step.hintText));
        }

        /// <summary>
        /// One sequence drives the hand AND the highlighted widget, so they can never drift out of
        /// phase: the widget swells over exactly the window the hand spends pressing in, and
        /// settles over the window the hand spends lifting. Two separate loops used to slide apart
        /// and read as the button randomly changing size.
        /// Drives localScale only - LateUpdate rewrites sizeDelta and anchoredPosition every frame.
        /// </summary>
        private void BuildMotion()
        {
            KillMotion();
            if (_step == null || _hand == null) return;

            TutorialHandConfig hand = _step.hand;

            _hand.anchoredPosition = Vector2.zero;
            _hand.localScale = Vector3.one;
            if (_handGroup != null) _handGroup.alpha = 1f;

            bool animatesHand = hand.animation != TutorialHandAnimation.None;
            bool animatesHighlight = _step.highlightPulse &&
                                     (_highlightClone != null ||
                                      (_focusRing != null && _step.showFocusRing));
            if (!animatesHand && !animatesHighlight) return;

            float duration = Mathf.Max(0.1f, hand.animationDuration);
            float press = duration * Mathf.Clamp(hand.pressRatio, 0.05f, 0.95f);
            float release = Mathf.Max(0.05f, duration - press);

            // SetUpdate(true): the beat keeps running even if a popup froze Time.timeScale.
            Sequence sequence = DOTween.Sequence()
                                       .SetUpdate(true)
                                       .SetTarget(this)
                                       .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (animatesHand) InsertHandBeat(sequence, hand, duration, press, release);
            if (animatesHighlight) InsertHighlightBeat(sequence, hand, press, release);

            if (hand.animationLoopDelay > 0f) sequence.AppendInterval(hand.animationLoopDelay);
            sequence.SetLoops(-1, LoopType.Restart);
            _motion = sequence;
        }

        private void InsertHandBeat(
            Sequence sequence, TutorialHandConfig hand, float duration, float press, float release)
        {
            float strength = Mathf.Max(0.05f, hand.animationStrength);

            switch (hand.animation)
            {
                case TutorialHandAnimation.Tap:
                case TutorialHandAnimation.Pulse:
                    // The pivot sits on the fingertip, so scaling alone reads as a real poke:
                    // the fingertip stays planted while the rest of the hand pulls toward it.
                    sequence.Insert(0f, _hand.DOScale(strength, press).SetEase(hand.pressEase))
                            .Insert(press, _hand.DOScale(1f, release).SetEase(hand.releaseEase));
                    break;

                case TutorialHandAnimation.Press:
                    // Holds at the bottom: the release only starts in the last third.
                    float hold = release * 0.5f;
                    sequence.Insert(0f, _hand.DOScale(strength, press).SetEase(hand.pressEase))
                            .Insert(press + hold,
                                    _hand.DOScale(1f, release - hold).SetEase(hand.releaseEase));
                    break;

                case TutorialHandAnimation.Swipe:
                    sequence.Insert(0f, _hand.DOAnchorPos(hand.swipeOffset, duration)
                                             .SetEase(Ease.InOutSine));
                    if (_handGroup != null)
                        sequence.Insert(0f, _handGroup.DOFade(0f, duration).SetEase(Ease.InQuad));
                    sequence.InsertCallback(duration, () =>
                    {
                        _hand.anchoredPosition = Vector2.zero;
                        if (_handGroup != null) _handGroup.alpha = 1f;
                    });
                    break;
            }
        }

        /// <summary>Swells on the press window, settles on the release window. Never the reverse.</summary>
        private void InsertHighlightBeat(
            Sequence sequence, TutorialHandConfig hand, float press, float release)
        {
            float peak = Mathf.Max(1f, _step.highlightPulseScale);

            if (_highlightClone != null)
            {
                sequence.Insert(0f, _highlightClone.DOScale(peak, press).SetEase(hand.pressEase))
                        .Insert(press, _highlightClone.DOScale(1f, release).SetEase(hand.releaseEase));
            }

            if (_focusRing != null && _step.showFocusRing)
            {
                sequence.Insert(0f, _focusRing.DOScale(peak, press).SetEase(hand.pressEase))
                        .Insert(press, _focusRing.DOScale(1f, release).SetEase(hand.releaseEase));
            }
        }

        private void KillMotion()
        {
            if (_motion != null)
            {
                _motion.Kill();
                _motion = null;
            }

            if (_focusRing != null) _focusRing.localScale = Vector3.one;
            if (_highlightClone != null) _highlightClone.localScale = Vector3.one;
            if (_hand != null) _hand.localScale = Vector3.one;
        }
        #endregion

        #region Cameras
        private float CanvasScale => _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

        private Camera CanvasCamera =>
            _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

        /// <summary>
        /// The gameplay camera is destroyed on every scene change, so it is looked up lazily
        /// rather than cached once at Awake.
        /// </summary>
        private Camera ResolveWorldCamera()
        {
            if (_worldCamera != null) return _worldCamera;

            _worldCamera = Camera.main;
            if (_worldCamera == null)
                _worldCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);

            return _worldCamera;
        }
        #endregion
    }
}
