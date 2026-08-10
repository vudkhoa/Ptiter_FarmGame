using System;
using System.Collections.Generic;
using System.Globalization;
using Core.Module.Quest;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyOwn.ServiceHarness
{
    public sealed class ProgressMilestoneView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _requirement;
        [SerializeField] private TMP_Text _currentProgress;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Image _starIcon;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _locked;
        [SerializeField] private RectTransform _bambooTrack;

        [Header("Board States")]
        [SerializeField] private Sprite _lockedBoardSprite;
        [SerializeField] private Sprite _claimableBoardSprite;
        [SerializeField] private Sprite _claimedBoardSprite;
        [SerializeField] private Image _boardImage;

        private string _milestoneId;
        private Action<string> _claim;
        private Tween _claimFeedback;
        private bool _bambooLayoutCached;
        private float _bambooBaseWidth;
        private float _bambooBaseX;
        private float _bambooBaseY;
        private float _bambooExtraWidth;
        private Image _bambooImage;
        private Sprite _bambooSourceSprite;
        private Sprite _runtimeBambooSprite;
        private Texture2D _runtimeBambooTexture;
        private RectTransform _boardPresentation;
        private bool _lockLayoutCached;
        private Vector3 _lockBaseLocalPosition;

        public string MilestoneId => _milestoneId;
        public RectTransform RewardAnchor =>
            _starIcon != null ? _starIcon.rectTransform : transform as RectTransform;

        public void ConfigureBambooTrack(
            bool ownsTrack,
            int milestoneCount,
            float itemPitch)
        {
            if (_bambooTrack == null) return;

            bool showTrack = ownsTrack && milestoneCount > 0 && itemPitch > 0f;
            _bambooTrack.gameObject.SetActive(showTrack);
            if (!showTrack) return;

            CacheBambooLayout();
            EnsureSeamlessBambooSprite();
            int extraMilestones = Mathf.Max(0, milestoneCount - 1);
            _bambooExtraWidth = itemPitch * extraMilestones;

            Vector2 size = _bambooTrack.sizeDelta;
            size.x = _bambooBaseWidth + _bambooExtraWidth;
            _bambooTrack.sizeDelta = size;
        }

        public void ConfigureBoardPosition(float offsetX)
        {
            RectTransform presentation = EnsureBoardPresentation();
            Vector2 position = presentation.anchoredPosition;
            position.x = offsetX;
            presentation.anchoredPosition = position;
        }

        private RectTransform EnsureBoardPresentation()
        {
            if (_boardPresentation != null) return _boardPresentation;

            GameObject presentationObject = new GameObject(
                "Board Presentation",
                typeof(RectTransform));
            _boardPresentation =
                presentationObject.GetComponent<RectTransform>();
            _boardPresentation.SetParent(transform, false);
            _boardPresentation.anchorMin = Vector2.zero;
            _boardPresentation.anchorMax = Vector2.one;
            _boardPresentation.offsetMin = Vector2.zero;
            _boardPresentation.offsetMax = Vector2.zero;
            _boardPresentation.pivot = new Vector2(0.5f, 0.5f);

            var boardChildren = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != _boardPresentation && child != _bambooTrack)
                    boardChildren.Add(child);
            }

            for (int i = 0; i < boardChildren.Count; i++)
                boardChildren[i].SetParent(_boardPresentation, false);

            _boardPresentation.SetAsFirstSibling();
            return _boardPresentation;
        }

        public void AttachBambooOverlay(RectTransform overlayParent)
        {
            if (_bambooTrack == null || overlayParent == null ||
                !_bambooTrack.gameObject.activeSelf)
                return;

            CacheBambooLayout();
            Vector3 targetWorldPosition =
                (transform as RectTransform).TransformPoint(
                    new Vector3(
                        _bambooBaseX + _bambooExtraWidth * 0.5f,
                        _bambooBaseY,
                        0f));

            EnsureIgnoredByLayout(_bambooTrack.gameObject);
            _bambooTrack.SetParent(overlayParent, false);
            _bambooTrack.position = targetWorldPosition;
            _bambooTrack.SetAsLastSibling();
        }

        private void EnsureSeamlessBambooSprite()
        {
            if (_runtimeBambooSprite != null) return;

            _bambooImage = _bambooTrack.GetComponent<Image>();
            if (_bambooImage == null || _bambooImage.sprite == null) return;

            _bambooSourceSprite = _bambooImage.sprite;
            _runtimeBambooTexture = CopyTexture(_bambooSourceSprite.texture);
            if (_runtimeBambooTexture == null) return;

            MakeHorizontalEdgesSeamless(_runtimeBambooTexture);
            Vector2 pivot = new Vector2(
                _bambooSourceSprite.pivot.x / _bambooSourceSprite.rect.width,
                _bambooSourceSprite.pivot.y / _bambooSourceSprite.rect.height);
            _runtimeBambooSprite = Sprite.Create(
                _runtimeBambooTexture,
                new Rect(
                    0f,
                    0f,
                    _runtimeBambooTexture.width,
                    _runtimeBambooTexture.height),
                pivot,
                _bambooSourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                _bambooSourceSprite.border);
            _runtimeBambooSprite.name =
                $"{_bambooSourceSprite.name} (Runtime Seamless)";
            _runtimeBambooSprite.hideFlags = HideFlags.DontSave;
            _runtimeBambooTexture.name =
                $"{_bambooSourceSprite.texture.name} (Runtime Seamless)";
            _runtimeBambooTexture.hideFlags = HideFlags.DontSave;
            _bambooImage.sprite = _runtimeBambooSprite;
            _bambooImage.type = Image.Type.Tiled;
        }

        private static Texture2D CopyTexture(Texture source)
        {
            if (source == null) return null;

            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                var copy = new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    filterMode = source.filterMode,
                    wrapMode = TextureWrapMode.Clamp
                };
                copy.ReadPixels(
                    new Rect(0f, 0f, source.width, source.height),
                    0,
                    0,
                    false);
                copy.Apply(false, false);
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static void MakeHorizontalEdgesSeamless(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            if (width < 8 || height < 8) return;

            Color32[] source = texture.GetPixels32();
            Color32[] output = (Color32[])source.Clone();
            int edgeInset = Mathf.Clamp(width / 156, 2, 16);
            int leftBlend = Mathf.Clamp(width / 32, 16, width / 4);
            int rightBlend = Mathf.Clamp(width / 12, 32, width / 3);
            float verticalShift = Mathf.Clamp(height / 26f, 2f, 12f);

            for (int y = 0; y < height; y++)
            {
                Color32 seam = source[y * width + edgeInset];
                for (int i = 0; i < leftBlend; i++)
                {
                    float t = Smooth01(i / (float)(leftBlend - 1));
                    int index = y * width + i;
                    output[index] = BlendPremultiplied(
                        seam,
                        source[index],
                        t);
                }

                for (int i = 0; i < rightBlend; i++)
                {
                    float t = Smooth01(i / (float)(rightBlend - 1));
                    int x = width - rightBlend + i;
                    float sourceY = y - verticalShift * t;
                    Color32 shifted = SampleVertical(
                        source,
                        width,
                        height,
                        x,
                        sourceY);
                    output[y * width + x] = BlendPremultiplied(
                        shifted,
                        seam,
                        t);
                }
            }

            texture.SetPixels32(output);
            texture.Apply(false, false);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Color32 SampleVertical(
            Color32[] pixels,
            int width,
            int height,
            int x,
            float y)
        {
            if (y < 0f || y > height - 1f)
                return new Color32(0, 0, 0, 0);

            int y0 = Mathf.FloorToInt(y);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float t = y - y0;
            return BlendPremultiplied(
                pixels[y0 * width + x],
                pixels[y1 * width + x],
                t);
        }

        private static Color32 BlendPremultiplied(
            Color32 from,
            Color32 to,
            float t)
        {
            float fromAlpha = from.a / 255f;
            float toAlpha = to.a / 255f;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            if (alpha <= 0.0001f) return new Color32(0, 0, 0, 0);

            float red = Mathf.Lerp(
                from.r * fromAlpha,
                to.r * toAlpha,
                t) / alpha;
            float green = Mathf.Lerp(
                from.g * fromAlpha,
                to.g * toAlpha,
                t) / alpha;
            float blue = Mathf.Lerp(
                from.b * fromAlpha,
                to.b * toAlpha,
                t) / alpha;
            return new Color(
                red / 255f,
                green / 255f,
                blue / 255f,
                alpha);
        }

        public void AttachLockOverlay(RectTransform overlayParent)
        {
            if (_locked == null || overlayParent == null) return;

            RectTransform presentation = EnsureBoardPresentation();
            RectTransform lockRect = _locked.transform as RectTransform;
            if (lockRect == null) return;

            if (!_lockLayoutCached)
            {
                _lockBaseLocalPosition = lockRect.localPosition;
                _lockLayoutCached = true;
            }

            Vector3 targetWorldPosition =
                presentation.TransformPoint(_lockBaseLocalPosition);
            EnsureIgnoredByLayout(_locked);
            _locked.name = string.IsNullOrWhiteSpace(_milestoneId)
                ? $"Lock - {name}"
                : $"Lock - {_milestoneId}";
            lockRect.SetParent(overlayParent, false);
            lockRect.position = targetWorldPosition;
            lockRect.SetAsLastSibling();
        }

        public void HideDetachedLock()
        {
            if (_locked != null &&
                _boardPresentation != null &&
                _locked.transform.parent != _boardPresentation)
                _locked.SetActive(false);
        }

        private static void EnsureIgnoredByLayout(GameObject target)
        {
            LayoutElement layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = target.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        private void CacheBambooLayout()
        {
            if (_bambooLayoutCached) return;

            _bambooBaseWidth = _bambooTrack.sizeDelta.x;
            _bambooBaseX = _bambooTrack.anchoredPosition.x;
            _bambooBaseY = _bambooTrack.anchoredPosition.y;
            _bambooLayoutCached = true;
        }

        public void Bind(ProgressMilestoneViewData data, Action<string> claim)
        {
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(OnClaimClicked);

            _milestoneId = data?.MilestoneId;
            _claim = claim;
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (_requirement != null)
                _requirement.text =
                    $"TÍCH {FormatNumber(data.RequiredCoins)} XU";
            if (_currentProgress != null)
                _currentProgress.text =
                    $"{FormatNumber(data.CurrentCoins)}/{FormatNumber(data.RequiredCoins)}";
            if (_reward != null)
                _reward.text = data.StarReward.ToString();

            bool locked =
                data.ClaimState == ProgressMilestoneClaimState.Locked;
            bool claimable =
                data.ClaimState == ProgressMilestoneClaimState.Claimable;
            if (_locked != null) _locked.SetActive(locked);
            if (_boardImage != null)
            {
                Sprite boardSprite = data.ClaimState switch
                {
                    ProgressMilestoneClaimState.Locked => _lockedBoardSprite,
                    ProgressMilestoneClaimState.Claimable => _claimableBoardSprite,
                    ProgressMilestoneClaimState.Claimed => _claimedBoardSprite,
                    _ => null
                };

                if (boardSprite != null)
                    _boardImage.sprite = boardSprite;
            }
            if (_claimButton != null)
            {
                _claimButton.interactable = claimable;
                _claimButton.onClick.AddListener(OnClaimClicked);
            }
        }

        private void OnClaimClicked()
        {
            if (!string.IsNullOrWhiteSpace(_milestoneId))
                _claim?.Invoke(_milestoneId);
        }

        public void PlayClaimFeedback()
        {
            RectTransform target = transform as RectTransform;
            if (target == null) return;

            _claimFeedback?.Kill(false);
            _claimFeedback = target
                .DOPunchScale(
                    Vector3.one * 0.06f,
                    0.20f,
                    4,
                    0.35f)
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => _claimFeedback = null);
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        }

        private void OnDestroy()
        {
            _claimFeedback?.Kill(false);
            _claimFeedback = null;
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(OnClaimClicked);

            if (_runtimeBambooSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeBambooSprite);
                else
                    DestroyImmediate(_runtimeBambooSprite);
            }

            if (_runtimeBambooTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeBambooTexture);
                else
                    DestroyImmediate(_runtimeBambooTexture);
            }
        }
    }
}
