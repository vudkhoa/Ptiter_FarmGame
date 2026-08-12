using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest.Cooking.UI
{
    [DisallowMultipleComponent]
    public sealed class FoodCookingPanelView : MonoBehaviour
    {
        private const float IntroEndTime = 1.65f;
        private const float CompletionEndTime = 1.30f;

        [Header("Stages")]
        [SerializeField] private RectTransform _detailStage;
        [SerializeField] private CanvasGroup _detailCanvasGroup;
        [SerializeField] private RectTransform _animationStage;

        [Header("Recipe Detail")]
        [SerializeField] private Image _dishIcon;
        [SerializeField] private TMP_Text _dishName;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Image _porkIngredientIcon;
        [SerializeField] private TMP_Text _porkIngredientName;
        [SerializeField] private TMP_Text _porkOwnedRequired;
        [SerializeField] private Image _wheatIngredientIcon;
        [SerializeField] private TMP_Text _wheatIngredientName;
        [SerializeField] private TMP_Text _wheatOwnedRequired;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _craftableText;
        [SerializeField] private TMP_Text _quantityValue;
        [SerializeField] private Color _insufficientIngredientColor =
            new Color(0.72f, 0.20f, 0.13f, 1f);

        [Header("Animation Visuals")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private RectTransform _impactRing;
        [SerializeField] private RectTransform _fireGlow;
        [SerializeField] private RectTransform _potBody;
        [SerializeField] private RectTransform _potLid;
        [SerializeField] private RectTransform _steamRoot;
        [SerializeField] private RectTransform[] _steam = new RectTransform[3];
        [SerializeField] private RectTransform _flyingPork;
        [SerializeField] private RectTransform _flyingWheat;
        [SerializeField] private RectTransform _resultIcon;

        [Header("Input")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _cookButton;
        [SerializeField] private Button _replayStoryButton;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _popClip;
        [SerializeField] private AudioClip _sizzleClip;
        [SerializeField] private AudioClip _confirmClip;
        [SerializeField, Range(0f, 1f)] private float _popVolume = 0.58f;
        [SerializeField, Range(0f, 1f)] private float _sizzleVolume = 0.32f;
        [SerializeField, Range(0f, 1f)] private float _confirmVolume = 0.68f;

        private Sequence _mainSequence;
        private Sequence[] _steamSequences;
        private Tween _fireScaleLoop;
        private Tween _fireFadeLoop;

        private Vector2 _potPosition;
        private Vector2 _lidPosition;
        private Vector2 _porkPosition;
        private Vector2 _wheatPosition;
        private Vector2 _impactPosition;
        private Vector2 _firePosition;
        private Vector2 _resultPosition;
        private Vector2[] _steamPositions;

        private Vector3 _potScale;
        private Vector3 _lidScale;
        private Vector3 _porkScale;
        private Vector3 _wheatScale;
        private Vector3 _impactScale;
        private Vector3 _fireScale;
        private Vector3 _resultScale;
        private Vector3[] _steamScales;

        private Quaternion _potRotation;
        private Quaternion _lidRotation;
        private Quaternion _porkRotation;
        private Quaternion _wheatRotation;
        private Quaternion _resultRotation;

        private Color _potColor;
        private Color _lidColor;
        private Color _porkColor;
        private Color _wheatColor;
        private Color _impactColor;
        private Color _fireColor;
        private Color _resultColor;
        private Color _statusColor;
        private Color[] _steamColors;

        private Image _potImage;
        private Image _lidImage;
        private Image _porkImage;
        private Image _wheatImage;
        private Image _impactImage;
        private Image _fireImage;
        private Image _resultImage;
        private Image[] _steamImages;
        private Color _porkOwnedRequiredColor;
        private Color _wheatOwnedRequiredColor;
        private bool _hasCachedDetailColors;

        private bool _hasCachedAuthoredState;
        private bool _replayStoryAvailable;

        public float IntroDuration => IntroEndTime;
        public float CompletionDuration => CompletionEndTime;
        public bool IsTransitionPlaying =>
            _mainSequence != null && _mainSequence.IsActive() &&
            _mainSequence.IsPlaying();

        public event Action IntroCompleted;
        public event Action CompletionFinished;
        public event Action CloseRequested;
        public event Action MinusRequested;
        public event Action PlusRequested;
        public event Action CookRequested;
        public event Action ReplayStoryRequested;

        private void Awake()
        {
            ResolveReferences();
            CacheAuthoredState();
            ShowDetailImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheAuthoredState();
            RegisterInput();

            if (!Application.isPlaying)
                return;

            ShowDetailImmediate();
        }

        private void RegisterInput()
        {
            RegisterButton(_closeButton, NotifyCloseRequested);
            RegisterButton(_minusButton, NotifyMinusRequested);
            RegisterButton(_plusButton, NotifyPlusRequested);
            RegisterButton(_cookButton, NotifyCookRequested);
            RegisterButton(
                _replayStoryButton,
                NotifyReplayStoryRequested);
        }

        private void UnregisterInput()
        {
            UnregisterButton(_closeButton, NotifyCloseRequested);
            UnregisterButton(_minusButton, NotifyMinusRequested);
            UnregisterButton(_plusButton, NotifyPlusRequested);
            UnregisterButton(_cookButton, NotifyCookRequested);
            UnregisterButton(
                _replayStoryButton,
                NotifyReplayStoryRequested);
        }

        private void NotifyCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void NotifyMinusRequested()
        {
            MinusRequested?.Invoke();
        }

        private void NotifyPlusRequested()
        {
            PlusRequested?.Invoke();
        }

        private void NotifyCookRequested()
        {
            CookRequested?.Invoke();
        }

        private void NotifyReplayStoryRequested()
        {
            ReplayStoryRequested?.Invoke();
        }

        private static void RegisterButton(
            Button button,
            UnityEngine.Events.UnityAction listener)
        {
            if (button == null) return;
            button.onClick.RemoveListener(listener);
            button.onClick.AddListener(listener);
        }

        private static void UnregisterButton(
            Button button,
            UnityEngine.Events.UnityAction listener)
        {
            if (button != null)
                button.onClick.RemoveListener(listener);
        }

        public void RenderDetail(
            CookingRecipeState state,
            int requestedQuantity)
        {
            ResolveReferences();
            CacheDetailColors();
            if (state == null) return;

            int quantity = Mathf.Clamp(
                requestedQuantity,
                1,
                Mathf.Max(1, state.MaxQuantity));
            if (_dishIcon != null)
            {
                _dishIcon.sprite = state.DishSprite;
                _dishIcon.enabled = state.DishSprite != null;
            }
            SetText(_dishName, state.DisplayName);
            SetText(_description, state.Description);

            CookingIngredientState pork =
                state.Ingredients != null && state.Ingredients.Count > 0
                    ? state.Ingredients[0]
                    : null;
            CookingIngredientState wheat =
                state.Ingredients != null && state.Ingredients.Count > 1
                    ? state.Ingredients[1]
                    : null;
            RenderIngredient(
                pork,
                _porkIngredientIcon,
                _porkIngredientName,
                _porkOwnedRequired,
                _porkOwnedRequiredColor);
            RenderIngredient(
                wheat,
                _wheatIngredientIcon,
                _wheatIngredientName,
                _wheatOwnedRequired,
                _wheatOwnedRequiredColor);

            int totalSeconds = Mathf.Max(0, state.SecondsPerItem * quantity);
            SetText(
                _timeText,
                $"THỜI GIAN NẤU: {totalSeconds / 60:00}:{totalSeconds % 60:00}");
            SetText(_craftableText, $"CÓ THỂ NẤU: {state.MaxCraftable}");
            SetText(_quantityValue, quantity.ToString());

            bool idle = !state.IsCooking && !state.IsBusyWithOtherRecipe;
            int maxSelectable = Mathf.Min(
                Mathf.Max(1, state.MaxQuantity),
                Mathf.Max(1, state.MaxCraftable));
            if (_minusButton != null)
                _minusButton.interactable = idle && quantity > 1;
            if (_plusButton != null)
            {
                _plusButton.interactable =
                    idle && quantity < maxSelectable;
            }
            if (_cookButton != null)
                _cookButton.interactable = idle && state.CanStart;
        }

        private void RenderIngredient(
            CookingIngredientState ingredient,
            Image icon,
            TMP_Text displayName,
            TMP_Text ownedRequired,
            Color authoredColor)
        {
            bool exists = ingredient != null;
            if (icon != null)
            {
                icon.sprite = ingredient?.Icon;
                icon.enabled = exists && ingredient.Icon != null;
            }
            SetText(displayName, exists ? ingredient.DisplayName : string.Empty);
            SetText(
                ownedRequired,
                exists
                    ? $"{ingredient.OwnedAmount} / {ingredient.RequiredAmount}"
                    : string.Empty);
            if (ownedRequired != null)
            {
                ownedRequired.color = exists && !ingredient.HasEnough
                    ? _insufficientIngredientColor
                    : authoredColor;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        public void ShowDetailImmediate()
        {
            ResolveReferences();
            CacheAuthoredState();
            KillAllTweens();
            StopAudio();

            if (_detailStage != null)
                _detailStage.gameObject.SetActive(true);

            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = 1f;
                _detailCanvasGroup.interactable = true;
                _detailCanvasGroup.blocksRaycasts = true;
            }

            if (_animationStage != null)
                _animationStage.gameObject.SetActive(false);

            RestoreAuthoredVisuals();
            SetDetailInput(true);
            SetReplayInput(true);
            SetCloseInput(true);
        }

        public void SetReplayStoryAvailable(bool available)
        {
            ResolveReferences();
            _replayStoryAvailable = available;
            if (_replayStoryButton == null) return;

            _replayStoryButton.gameObject.SetActive(available);
            _replayStoryButton.interactable = available;
        }

        public void PlayIntro(int quantity, int remainingSeconds)
        {
            ResolveReferences();
            CacheAuthoredState();
            KillAllTweens();
            StopAudio();

            quantity = Mathf.Max(1, quantity);
            remainingSeconds = Mathf.Max(0, remainingSeconds);

            if (_detailStage != null)
                _detailStage.gameObject.SetActive(true);
            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = 1f;
                _detailCanvasGroup.interactable = false;
                _detailCanvasGroup.blocksRaycasts = false;
            }

            if (_animationStage != null)
                _animationStage.gameObject.SetActive(true);

            SetDetailInput(false);
            SetReplayInput(false);
            SetCloseInput(false);
            PrepareIntroVisuals();
            UpdateCountdown(quantity, remainingSeconds);

            _mainSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_detailCanvasGroup != null)
            {
                _mainSequence.Insert(
                    0f,
                    _detailCanvasGroup
                        .DOFade(0f, 0.15f)
                        .SetEase(Ease.InQuad));
                _mainSequence.InsertCallback(
                    0.15f,
                    () => _detailStage.gameObject.SetActive(false));
            }

            BuildPotIntro(_mainSequence);
            BuildIngredientFlight(
                _mainSequence,
                _flyingPork,
                _porkImage,
                _porkPosition,
                _porkScale,
                _porkRotation,
                -22f,
                -18f,
                0.24f);
            BuildIngredientFlight(
                _mainSequence,
                _flyingWheat,
                _wheatImage,
                _wheatPosition,
                _wheatScale,
                _wheatRotation,
                22f,
                18f,
                0.43f);

            BuildLidImpact(_mainSequence);
            BuildFireAndSteam(_mainSequence);

            if (_statusText != null)
            {
                _mainSequence.Insert(
                    1.45f,
                    _statusText
                        .DOFade(_statusColor.a, 0.18f)
                        .SetEase(Ease.OutQuad));
                _mainSequence.Insert(
                    1.45f,
                    _statusText.rectTransform
                        .DOAnchorPosY(
                            _statusText.rectTransform.anchoredPosition.y + 10f,
                            0.18f)
                        .From()
                        .SetEase(Ease.OutCubic));
            }

            _mainSequence.InsertCallback(
                IntroEndTime,
                () =>
                {
                    _mainSequence = null;
                    SetCloseInput(true);
                    IntroCompleted?.Invoke();
                });
        }

        public void ShowCountdown(int quantity, int remainingSeconds)
        {
            ResolveReferences();
            CacheAuthoredState();
            KillAllTweens();
            StopAudio();

            if (_detailStage != null)
                _detailStage.gameObject.SetActive(false);
            if (_animationStage != null)
                _animationStage.gameObject.SetActive(true);

            RestoreCookingVisuals();
            SetDetailInput(false);
            SetReplayInput(true);
            SetCloseInput(true);
            UpdateCountdown(quantity, remainingSeconds);
            StartCookingLoops();
        }

        public void UpdateCountdown(int quantity, int remainingSeconds)
        {
            if (_statusText == null)
                return;

            quantity = Mathf.Max(1, quantity);
            remainingSeconds = Mathf.Max(0, remainingSeconds);
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;
            _statusText.text =
                $"ĐANG NẤU ×{quantity}  •  {minutes:00}:{seconds:00}";
        }

        public void PlayCompletion(int quantity)
        {
            ResolveReferences();
            CacheAuthoredState();
            KillAllTweens();
            StopAudio();

            quantity = Mathf.Max(1, quantity);

            if (_detailStage != null)
                _detailStage.gameObject.SetActive(false);
            if (_animationStage != null)
                _animationStage.gameObject.SetActive(true);

            SetDetailInput(false);
            SetReplayInput(false);
            SetCloseInput(false);
            PrepareCompletionVisuals(quantity);
            PlaySfx(_confirmClip, _confirmVolume);

            _mainSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_potLid != null)
            {
                Sequence lidHop = DOTween.Sequence();
                lidHop.Append(
                    _potLid
                        .DOAnchorPos(_lidPosition + Vector2.up * 58f, 0.11f)
                        .SetEase(Ease.OutQuad));
                lidHop.Join(
                    _potLid
                        .DOLocalRotate(
                            new Vector3(0f, 0f, 7f),
                            0.11f,
                            RotateMode.Fast)
                        .SetEase(Ease.OutQuad));
                lidHop.Append(
                    _potLid
                        .DOAnchorPos(_lidPosition, 0.09f)
                        .SetEase(Ease.InQuad));
                lidHop.Join(
                    _potLid
                        .DOLocalRotate(
                            _lidRotation.eulerAngles,
                            0.09f,
                            RotateMode.Fast)
                        .SetEase(Ease.InQuad));
                _mainSequence.Insert(0f, lidHop);
            }

            if (_resultIcon != null && _resultImage != null)
            {
                Sequence resultPop = DOTween.Sequence();
                resultPop.Append(
                    _resultIcon
                        .DOScale(_resultScale * 1.15f, 0.19f)
                        .SetEase(Ease.OutBack));
                resultPop.Append(
                    _resultIcon
                        .DOScale(_resultScale, 0.09f)
                        .SetEase(Ease.InOutSine));
                _mainSequence.Insert(0.10f, resultPop);
                _mainSequence.Insert(
                    0.10f,
                    _resultImage
                        .DOFade(_resultColor.a, 0.08f)
                        .SetEase(Ease.OutQuad));
            }

            if (_statusText != null)
            {
                _mainSequence.Insert(
                    0.18f,
                    _statusText
                        .DOFade(_statusColor.a, 0.23f)
                        .SetEase(Ease.OutQuad));
                _mainSequence.Insert(
                    0.18f,
                    _statusText.rectTransform
                        .DOScale(1.04f, 0.16f)
                        .From()
                        .SetEase(Ease.OutBack));
            }

            if (_resultIcon != null && _resultImage != null)
            {
                _mainSequence.Insert(
                    0.65f,
                    _resultIcon
                        .DOAnchorPos(
                            _resultPosition + new Vector2(560f, 260f),
                            0.45f)
                        .SetEase(Ease.InCubic));
                _mainSequence.Insert(
                    0.65f,
                    _resultIcon
                        .DOScale(_resultScale * 0.55f, 0.45f)
                        .SetEase(Ease.InCubic));
                _mainSequence.Insert(
                    0.76f,
                    _resultImage
                        .DOFade(0f, 0.34f)
                        .SetEase(Ease.InQuad));
            }

            _mainSequence.InsertCallback(
                1.12f,
                ShowDetailAfterCompletion);
            _mainSequence.InsertCallback(
                CompletionEndTime,
                () =>
                {
                    _mainSequence = null;
                    CompletionFinished?.Invoke();
                });
        }

        private void BuildPotIntro(Sequence sequence)
        {
            if (_potBody == null || _potImage == null)
                return;

            sequence.Insert(
                0.10f,
                _potImage
                    .DOFade(_potColor.a, 0.08f)
                    .SetEase(Ease.OutQuad));
            sequence.Insert(
                0.10f,
                _potBody
                    .DOAnchorPos(_potPosition, 0.24f)
                    .SetEase(Ease.OutCubic));

            Sequence pop = DOTween.Sequence();
            pop.Append(
                _potBody
                    .DOScale(_potScale * 1.08f, 0.18f)
                    .SetEase(Ease.OutBack));
            pop.Append(
                _potBody
                    .DOScale(_potScale, 0.06f)
                    .SetEase(Ease.InOutSine));
            sequence.Insert(0.10f, pop);
        }

        private void BuildIngredientFlight(
            Sequence sequence,
            RectTransform ingredient,
            Image image,
            Vector2 startPosition,
            Vector3 startScale,
            Quaternion startRotation,
            float arcDirection,
            float rotationDegrees,
            float startTime)
        {
            if (ingredient == null || image == null || _potBody == null)
                return;

            Vector2 end = _potPosition + new Vector2(arcDirection, 58f);
            Vector2 control =
                Vector2.Lerp(startPosition, end, 0.5f) +
                Vector2.up * 118f;

            Tween curvedFlight = DOTween
                .To(
                    () => 0f,
                    value =>
                    {
                        float inverse = 1f - value;
                        ingredient.anchoredPosition =
                            inverse * inverse * startPosition +
                            2f * inverse * value * control +
                            value * value * end;
                    },
                    1f,
                    0.34f)
                .SetEase(Ease.InQuad);

            sequence.Insert(startTime, curvedFlight);
            sequence.Insert(
                startTime,
                ingredient
                    .DOScale(startScale * 0.55f, 0.34f)
                    .SetEase(Ease.InQuad));
            sequence.Insert(
                startTime,
                ingredient
                    .DOLocalRotate(
                        startRotation.eulerAngles +
                        new Vector3(0f, 0f, rotationDegrees),
                        0.34f,
                        RotateMode.Fast)
                    .SetEase(Ease.InOutSine));
            sequence.Insert(
                startTime + 0.25f,
                image
                    .DOFade(0f, 0.09f)
                    .SetEase(Ease.InQuad));
            sequence.InsertCallback(
                startTime + 0.34f,
                () => PlaySfx(_popClip, _popVolume));
        }

        private void BuildLidImpact(Sequence sequence)
        {
            if (_potLid != null && _lidImage != null)
            {
                sequence.Insert(
                    0.72f,
                    _lidImage
                        .DOFade(_lidColor.a, 0.06f)
                        .SetEase(Ease.OutQuad));
                sequence.Insert(
                    0.72f,
                    _potLid
                        .DOAnchorPos(_lidPosition, 0.28f)
                        .SetEase(Ease.InBack));
                sequence.Insert(
                    0.72f,
                    _potLid
                        .DOLocalRotate(
                            _lidRotation.eulerAngles,
                            0.28f,
                            RotateMode.Fast)
                        .SetEase(Ease.InQuad));
            }

            sequence.InsertCallback(
                0.96f,
                () =>
                {
                    if (_impactRing != null)
                        _impactRing.localScale = _impactScale * 0.62f;
                    SetGraphicAlpha(_impactImage, _impactColor.a);
                });

            if (_impactRing != null && _impactImage != null)
            {
                sequence.Insert(
                    0.96f,
                    _impactRing
                        .DOScale(_impactScale * 1.50f, 0.14f)
                        .SetEase(Ease.OutCubic));
                sequence.Insert(
                    0.98f,
                    _impactImage
                        .DOFade(0f, 0.12f)
                        .SetEase(Ease.InQuad));
            }

            if (_potBody != null)
            {
                sequence.Insert(
                    0.96f,
                    _potBody
                        .DOPunchScale(
                            new Vector3(0.06f, -0.035f, 0f),
                            0.17f,
                            5,
                            0.55f));
                sequence.Insert(
                    0.96f,
                    _potBody
                        .DOPunchRotation(
                            new Vector3(0f, 0f, 2.4f),
                            0.17f,
                            5,
                            0.45f));
            }
        }

        private void BuildFireAndSteam(Sequence sequence)
        {
            if (_fireGlow != null && _fireImage != null)
            {
                sequence.Insert(
                    1.02f,
                    _fireGlow
                        .DOScale(_fireScale, 0.23f)
                        .SetEase(Ease.OutBack));
                sequence.Insert(
                    1.02f,
                    _fireImage
                        .DOFade(
                            Mathf.Min(1f, Mathf.Max(0.68f, _fireColor.a)),
                            0.20f)
                        .SetEase(Ease.OutQuad));
            }

            sequence.InsertCallback(
                1.03f,
                () => PlaySfx(_sizzleClip, _sizzleVolume));
            sequence.InsertCallback(1.10f, StartCookingLoops);
        }

        private void PrepareIntroVisuals()
        {
            RestoreAuthoredVisuals();

            if (_potBody != null)
            {
                _potBody.anchoredPosition =
                    _potPosition + Vector2.down * 52f;
                _potBody.localScale = _potScale * 0.72f;
            }
            SetGraphicAlpha(_potImage, 0f);

            if (_potLid != null)
            {
                _potLid.anchoredPosition =
                    _lidPosition + Vector2.up * 170f;
                _potLid.localRotation =
                    Quaternion.Euler(
                        _lidRotation.eulerAngles +
                        new Vector3(0f, 0f, -9f));
            }
            SetGraphicAlpha(_lidImage, 0f);

            SetGraphicAlpha(_porkImage, _porkColor.a);
            SetGraphicAlpha(_wheatImage, _wheatColor.a);

            if (_impactRing != null)
                _impactRing.localScale = _impactScale * 0.62f;
            SetGraphicAlpha(_impactImage, 0f);

            if (_fireGlow != null)
                _fireGlow.localScale = _fireScale * 0.78f;
            SetGraphicAlpha(_fireImage, 0f);

            HideSteam();
            SetGraphicAlpha(_resultImage, 0f);
            if (_resultIcon != null)
                _resultIcon.localScale = Vector3.zero;

            SetGraphicAlpha(_statusText, 0f);
        }

        private void RestoreCookingVisuals()
        {
            RestoreAuthoredVisuals();

            SetGraphicAlpha(_porkImage, 0f);
            SetGraphicAlpha(_wheatImage, 0f);
            SetGraphicAlpha(_impactImage, 0f);
            SetGraphicAlpha(_resultImage, 0f);
            SetGraphicAlpha(_statusText, _statusColor.a);

            if (_resultIcon != null)
                _resultIcon.localScale = Vector3.zero;

            if (_fireGlow != null)
                _fireGlow.localScale = _fireScale;
            SetGraphicAlpha(
                _fireImage,
                Mathf.Min(1f, Mathf.Max(0.68f, _fireColor.a)));

            HideSteam();
        }

        private void PrepareCompletionVisuals(int quantity)
        {
            RestoreCookingVisuals();
            StopCookingLoops();

            SetGraphicAlpha(_fireImage, 0.48f);
            HideSteam();

            if (_resultIcon != null)
            {
                _resultIcon.anchoredPosition = _resultPosition;
                _resultIcon.localScale = Vector3.zero;
                _resultIcon.localRotation = _resultRotation;
            }
            SetGraphicAlpha(_resultImage, 0f);

            if (_statusText != null)
            {
                _statusText.text = $"HOÀN THÀNH ×{quantity}";
                _statusText.rectTransform.localScale = Vector3.one;
            }
            SetGraphicAlpha(_statusText, 0f);
        }

        private void StartCookingLoops()
        {
            StopCookingLoops();

            if (_steam == null || _steam.Length == 0)
                return;

            _steamSequences = new Sequence[_steam.Length];

            for (int i = 0; i < _steam.Length; i++)
            {
                RectTransform steamRect = _steam[i];
                Image steamImage = _steamImages[i];
                if (steamRect == null || steamImage == null)
                    continue;

                int index = i;
                float rise = 21f + index * 4f;
                float phaseDelay = index * 0.21f;
                float restDelay = 0.24f + index * 0.05f;
                float peakAlpha =
                    Mathf.Clamp01(
                        Mathf.Max(0.52f, _steamColors[index].a));

                Sequence loop = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetTarget(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

                loop.AppendInterval(phaseDelay);
                loop.AppendCallback(
                    () =>
                    {
                        steamRect.anchoredPosition =
                            _steamPositions[index];
                        steamRect.localScale =
                            _steamScales[index] * 0.92f;
                        SetGraphicAlpha(steamImage, 0.18f);
                    });
                loop.Append(
                    steamRect
                        .DOAnchorPos(
                            _steamPositions[index] + Vector2.up * rise,
                            0.78f)
                        .SetEase(Ease.OutSine));
                loop.Join(
                    steamRect
                        .DOScale(_steamScales[index] * 1.06f, 0.78f)
                        .SetEase(Ease.OutSine));
                loop.Insert(
                    phaseDelay,
                    steamImage
                        .DOFade(peakAlpha, 0.24f)
                        .SetEase(Ease.OutQuad));
                loop.Insert(
                    phaseDelay + 0.27f,
                    steamImage
                        .DOFade(0f, 0.51f)
                        .SetEase(Ease.InQuad));
                loop.AppendInterval(restDelay);
                loop.SetLoops(-1, LoopType.Restart);
                _steamSequences[index] = loop;
            }

            if (_fireGlow != null)
            {
                _fireScaleLoop = _fireGlow
                    .DOScale(_fireScale * 1.025f, 0.48f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetTarget(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (_fireImage != null)
            {
                float lowAlpha =
                    Mathf.Clamp01(
                        Mathf.Max(0.58f, _fireColor.a * 0.78f));
                _fireFadeLoop = _fireImage
                    .DOFade(lowAlpha, 0.52f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetTarget(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        private void StopCookingLoops()
        {
            if (_steamSequences != null)
            {
                for (int i = 0; i < _steamSequences.Length; i++)
                {
                    _steamSequences[i]?.Kill(false);
                    _steamSequences[i] = null;
                }
            }

            _fireScaleLoop?.Kill(false);
            _fireScaleLoop = null;
            _fireFadeLoop?.Kill(false);
            _fireFadeLoop = null;
        }

        private void ShowDetailAfterCompletion()
        {
            StopCookingLoops();
            StopAudio();

            if (_detailStage != null)
                _detailStage.gameObject.SetActive(true);
            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = 0f;
                _detailCanvasGroup.interactable = true;
                _detailCanvasGroup.blocksRaycasts = true;
                _detailCanvasGroup
                    .DOFade(1f, 0.16f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (_animationStage != null)
                _animationStage.gameObject.SetActive(false);

            RestoreAuthoredVisuals();
            SetDetailInput(true);
            SetReplayInput(true);
            SetCloseInput(true);
        }

        private void KillAllTweens()
        {
            _mainSequence?.Kill(false);
            _mainSequence = null;
            StopCookingLoops();
            DOTween.Kill(this, false);
        }

        private void StopAudio()
        {
            if (_audioSource == null)
                return;

            _audioSource.Stop();
            _audioSource.clip = null;
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
                return;

            _audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void SetDetailInput(bool interactable)
        {
            if (_minusButton != null)
                _minusButton.interactable = interactable;
            if (_plusButton != null)
                _plusButton.interactable = interactable;
            if (_cookButton != null)
                _cookButton.interactable = interactable;
        }

        private void SetReplayInput(bool interactable)
        {
            if (_replayStoryButton != null)
            {
                _replayStoryButton.interactable =
                    _replayStoryAvailable && interactable;
            }
        }

        private void SetCloseInput(bool interactable)
        {
            if (_closeButton != null)
                _closeButton.interactable = interactable;
        }

        private void HideSteam()
        {
            if (_steam == null)
                return;

            for (int i = 0; i < _steam.Length; i++)
            {
                if (_steam[i] != null)
                {
                    _steam[i].anchoredPosition =
                        _steamPositions != null &&
                        i < _steamPositions.Length
                            ? _steamPositions[i]
                            : _steam[i].anchoredPosition;
                    _steam[i].localScale =
                        _steamScales != null &&
                        i < _steamScales.Length
                            ? _steamScales[i]
                            : _steam[i].localScale;
                }

                if (_steamImages != null && i < _steamImages.Length)
                    SetGraphicAlpha(_steamImages[i], 0f);
            }
        }

        private void RestoreAuthoredVisuals()
        {
            if (!_hasCachedAuthoredState)
                return;

            RestoreRect(
                _potBody,
                _potPosition,
                _potScale,
                _potRotation);
            RestoreRect(
                _potLid,
                _lidPosition,
                _lidScale,
                _lidRotation);
            RestoreRect(
                _flyingPork,
                _porkPosition,
                _porkScale,
                _porkRotation);
            RestoreRect(
                _flyingWheat,
                _wheatPosition,
                _wheatScale,
                _wheatRotation);
            RestoreRect(
                _impactRing,
                _impactPosition,
                _impactScale,
                Quaternion.identity);
            RestoreRect(
                _fireGlow,
                _firePosition,
                _fireScale,
                Quaternion.identity);
            RestoreRect(
                _resultIcon,
                _resultPosition,
                _resultScale,
                _resultRotation);

            SetGraphicColor(_potImage, _potColor);
            SetGraphicColor(_lidImage, _lidColor);
            SetGraphicColor(_porkImage, _porkColor);
            SetGraphicColor(_wheatImage, _wheatColor);
            SetGraphicColor(_impactImage, _impactColor);
            SetGraphicColor(_fireImage, _fireColor);
            SetGraphicColor(_resultImage, _resultColor);
            SetGraphicColor(_statusText, _statusColor);

            if (_statusText != null)
                _statusText.rectTransform.localScale = Vector3.one;

            if (_steam != null)
            {
                for (int i = 0; i < _steam.Length; i++)
                {
                    if (_steam[i] != null)
                    {
                        _steam[i].anchoredPosition = _steamPositions[i];
                        _steam[i].localScale = _steamScales[i];
                    }

                    if (_steamImages[i] != null)
                        _steamImages[i].color = _steamColors[i];
                }
            }
        }

        private static void RestoreRect(
            RectTransform target,
            Vector2 position,
            Vector3 scale,
            Quaternion rotation)
        {
            if (target == null)
                return;

            target.anchoredPosition = position;
            target.localScale = scale;
            target.localRotation = rotation;
        }

        private static void SetGraphicColor(
            Graphic graphic,
            Color color)
        {
            if (graphic != null)
                graphic.color = color;
        }

        private static void SetGraphicAlpha(
            Graphic graphic,
            float alpha)
        {
            if (graphic == null)
                return;

            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        private void ResolveReferences()
        {
            // UnityEngine.Object can be a serialized "fake null". Do not use
            // ??= here because it checks CLR null and skips those references.
            if (_detailStage == null)
                _detailStage = FindRect("WindowRoot/DetailStage");
            if (_animationStage == null)
            {
                _animationStage =
                    FindRect("WindowRoot/AnimationStage");
            }

            if (_detailStage != null && _detailCanvasGroup == null)
            {
                _detailCanvasGroup =
                    _detailStage.GetComponent<CanvasGroup>();
            }

            if (_dishIcon == null)
            {
                _dishIcon = FindComponent<Image>(
                    "WindowRoot/DetailStage/RecipeInfoPanel/" +
                    "DishIconFrame/DishIcon");
            }
            if (_dishName == null)
            {
                _dishName = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/RecipeInfoPanel/DishName");
            }
            if (_description == null)
            {
                _description = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/RecipeInfoPanel/Description");
            }
            if (_porkIngredientIcon == null)
            {
                _porkIngredientIcon = FindComponent<Image>(
                    "WindowRoot/DetailStage/IngredientPanel/PorkSlot/Icon");
            }
            if (_porkIngredientName == null)
            {
                _porkIngredientName = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/PorkSlot/Name");
            }
            if (_porkOwnedRequired == null)
            {
                _porkOwnedRequired = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/PorkSlot/" +
                    "OwnedRequiredText");
            }
            if (_wheatIngredientIcon == null)
            {
                _wheatIngredientIcon = FindComponent<Image>(
                    "WindowRoot/DetailStage/IngredientPanel/WheatSlot/Icon");
            }
            if (_wheatIngredientName == null)
            {
                _wheatIngredientName = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/WheatSlot/Name");
            }
            if (_wheatOwnedRequired == null)
            {
                _wheatOwnedRequired = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/WheatSlot/" +
                    "OwnedRequiredText");
            }
            if (_timeText == null)
            {
                _timeText = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/TimeText");
            }
            if (_craftableText == null)
            {
                _craftableText = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/CraftableText");
            }
            if (_quantityValue == null)
            {
                _quantityValue = FindComponent<TMP_Text>(
                    "WindowRoot/DetailStage/IngredientPanel/" +
                    "QuantitySelector/QuantityValue/Value");
            }

            if (_statusText == null)
            {
                _statusText = FindComponent<TMP_Text>(
                    "WindowRoot/AnimationStage/StatusText");
            }
            if (_impactRing == null)
            {
                _impactRing =
                    FindRect("WindowRoot/AnimationStage/ImpactRing");
            }
            if (_fireGlow == null)
            {
                _fireGlow =
                    FindRect("WindowRoot/AnimationStage/FireGlow");
            }
            if (_potBody == null)
            {
                _potBody =
                    FindRect("WindowRoot/AnimationStage/PotBody");
            }
            if (_potLid == null)
            {
                _potLid =
                    FindRect("WindowRoot/AnimationStage/PotLid");
            }
            if (_steamRoot == null)
            {
                _steamRoot =
                    FindRect("WindowRoot/AnimationStage/SteamRoot");
            }
            if (_flyingPork == null)
            {
                _flyingPork =
                    FindRect("WindowRoot/AnimationStage/FlyingPork");
            }
            if (_flyingWheat == null)
            {
                _flyingWheat =
                    FindRect("WindowRoot/AnimationStage/FlyingWheat");
            }
            if (_resultIcon == null)
            {
                _resultIcon =
                    FindRect("WindowRoot/AnimationStage/ResultIcon");
            }

            if (_steam == null || _steam.Length != 3)
                _steam = new RectTransform[3];

            if (_steam[0] == null)
            {
                _steam[0] = FindRect(
                    "WindowRoot/AnimationStage/SteamRoot/Steam1");
            }
            if (_steam[1] == null)
            {
                _steam[1] = FindRect(
                    "WindowRoot/AnimationStage/SteamRoot/Steam2");
            }
            if (_steam[2] == null)
            {
                _steam[2] = FindRect(
                    "WindowRoot/AnimationStage/SteamRoot/Steam3");
            }

            if (_closeButton == null)
            {
                _closeButton =
                    FindComponent<Button>("WindowRoot/CloseButton");
            }
            if (_minusButton == null)
            {
                _minusButton = FindComponent<Button>(
                    "WindowRoot/DetailStage/IngredientPanel/" +
                    "QuantitySelector/MinusButton");
            }
            if (_plusButton == null)
            {
                _plusButton = FindComponent<Button>(
                    "WindowRoot/DetailStage/IngredientPanel/" +
                    "QuantitySelector/PlusButton");
            }
            if (_cookButton == null)
            {
                _cookButton = FindComponent<Button>(
                    "WindowRoot/DetailStage/IngredientPanel/CookButton");
            }
            if (_replayStoryButton == null)
            {
                _replayStoryButton = FindComponent<Button>(
                    "WindowRoot/ReplayStoryButton");
            }
            if (_audioSource == null)
            {
                _audioSource =
                    FindComponent<AudioSource>("AudioSource");
            }

            if (_potImage == null)
                _potImage = GetImage(_potBody);
            if (_lidImage == null)
                _lidImage = GetImage(_potLid);
            if (_porkImage == null)
                _porkImage = GetImage(_flyingPork);
            if (_wheatImage == null)
                _wheatImage = GetImage(_flyingWheat);
            if (_impactImage == null)
                _impactImage = GetImage(_impactRing);
            if (_fireImage == null)
                _fireImage = GetImage(_fireGlow);
            if (_resultImage == null)
                _resultImage = GetImage(_resultIcon);

            if (_steamImages == null || _steamImages.Length != 3)
                _steamImages = new Image[3];
            for (int i = 0; i < _steam.Length; i++)
            {
                if (_steamImages[i] == null)
                    _steamImages[i] = GetImage(_steam[i]);
            }

            CacheDetailColors();
        }

        private void CacheDetailColors()
        {
            if (_hasCachedDetailColors ||
                _porkOwnedRequired == null ||
                _wheatOwnedRequired == null)
                return;

            _porkOwnedRequiredColor = _porkOwnedRequired.color;
            _wheatOwnedRequiredColor = _wheatOwnedRequired.color;
            _hasCachedDetailColors = true;
        }

        private void CacheAuthoredState()
        {
            if (_hasCachedAuthoredState ||
                _potBody == null ||
                _potLid == null ||
                _flyingPork == null ||
                _flyingWheat == null ||
                _impactRing == null ||
                _fireGlow == null ||
                _resultIcon == null ||
                _statusText == null)
            {
                return;
            }

            _potPosition = _potBody.anchoredPosition;
            _lidPosition = _potLid.anchoredPosition;
            _porkPosition = _flyingPork.anchoredPosition;
            _wheatPosition = _flyingWheat.anchoredPosition;
            _impactPosition = _impactRing.anchoredPosition;
            _firePosition = _fireGlow.anchoredPosition;
            _resultPosition = _resultIcon.anchoredPosition;

            _potScale = _potBody.localScale;
            _lidScale = _potLid.localScale;
            _porkScale = _flyingPork.localScale;
            _wheatScale = _flyingWheat.localScale;
            _impactScale = _impactRing.localScale;
            _fireScale = _fireGlow.localScale;
            _resultScale = _resultIcon.localScale;

            _potRotation = _potBody.localRotation;
            _lidRotation = _potLid.localRotation;
            _porkRotation = _flyingPork.localRotation;
            _wheatRotation = _flyingWheat.localRotation;
            _resultRotation = _resultIcon.localRotation;

            _potColor = GetColor(_potImage);
            _lidColor = GetColor(_lidImage);
            _porkColor = GetColor(_porkImage);
            _wheatColor = GetColor(_wheatImage);
            _impactColor = GetColor(_impactImage);
            _fireColor = GetColor(_fireImage);
            _resultColor = GetColor(_resultImage);
            _statusColor = GetColor(_statusText);

            _steamPositions = new Vector2[_steam.Length];
            _steamScales = new Vector3[_steam.Length];
            _steamColors = new Color[_steam.Length];

            for (int i = 0; i < _steam.Length; i++)
            {
                if (_steam[i] != null)
                {
                    _steamPositions[i] = _steam[i].anchoredPosition;
                    _steamScales[i] = _steam[i].localScale;
                }
                else
                {
                    _steamPositions[i] = Vector2.zero;
                    _steamScales[i] = Vector3.one;
                }

                _steamColors[i] = GetColor(_steamImages[i]);
            }

            _hasCachedAuthoredState = true;
        }

        private RectTransform FindRect(string path)
        {
            return transform.Find(path) as RectTransform;
        }

        private T FindComponent<T>(string path)
            where T : Component
        {
            Transform target = transform.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static Image GetImage(RectTransform target)
        {
            return target != null ? target.GetComponent<Image>() : null;
        }

        private static Color GetColor(Graphic graphic)
        {
            return graphic != null ? graphic.color : Color.white;
        }

        private void OnDisable()
        {
            UnregisterInput();
            KillAllTweens();
            StopAudio();
        }

        private void OnDestroy()
        {
            UnregisterInput();
            KillAllTweens();
            StopAudio();
        }
    }
}
