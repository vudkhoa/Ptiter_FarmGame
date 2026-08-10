using System;
using System.Collections.Generic;
using System.Globalization;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using Core.Module.Quest;
using Core.Module.Quest.Utils;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MyOwn.ServiceHarness
{
    [DisallowMultipleComponent]
    public sealed class QuestWindowController :
        WindowController,
        IOnBeforeWindowOpen,
        IOnWindowClosed
    {
        [Header("Navigation")]
        [SerializeField] private Button _dailyTab;
        [SerializeField] private Button _progressTab;
        [SerializeField] private Button _foodTab;
        [SerializeField] private Button _closeButton;

        [Header("Shared Tab Visuals")]
        [SerializeField] private Image _dailyTabVisual;
        [SerializeField] private Image _progressTabVisual;
        [SerializeField] private Image _foodTabVisual;
        [SerializeField] private TMP_Text _dailyTabLabel;
        [SerializeField] private TMP_Text _progressTabLabel;
        [SerializeField] private TMP_Text _foodTabLabel;
        [SerializeField] private Sprite _selectedTabSprite;
        [SerializeField] private Sprite _inactiveTabSprite;
        [SerializeField] private Color _selectedTabTextColor = Color.white;
        [SerializeField] private Color _inactiveTabTextColor =
            new Color(0.55f, 0.30f, 0.24f, 1f);

        [Header("Panels")]
        [SerializeField] private GameObject _dailyPanel;
        [SerializeField] private GameObject _progressPlaceholder;
        [SerializeField] private GameObject _foodPlaceholder;

        [Header("Daily")]
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private TMP_Text _totalPoints;
        [SerializeField] private TMP_Text _lockedReason;
        [SerializeField] private ScrollRect _taskScroll;
        [SerializeField] private RectTransform _taskContent;
        [SerializeField] private QuestTaskItemView _taskTemplate;
        [SerializeField] private QuestMilestoneView[] _milestones;
        [SerializeField] private Image _dailyMilestoneFill;

        [Header("Progress")]
        [SerializeField] private Image _progressStarIcon;
        [SerializeField] private TMP_Text _progressStars;
        [SerializeField] private ScrollRect _progressScroll;
        [SerializeField] private RectTransform _progressContent;
        [SerializeField] private ProgressMilestoneView _progressMilestoneTemplate;
        [SerializeField] private RectTransform _progressLockOverlay;
        [SerializeField] private Vector3 _progressBoardCycleOffsets =
            new Vector3(0f, 24f, 45f);

        [Header("Reward feedback")]
        [SerializeField] private GameObject _rewardToast;
        [SerializeField] private TMP_Text _rewardToastText;

        private IDailyQuestService _dailyQuestService;
        private IProgressQuestService _progressQuestService;
        private IFoodRecipeService _foodRecipeService;
        private IPublisher<QuestToastRequestedPayload> _toastPublisher;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<QuestTaskItemView> _taskViews =
            new List<QuestTaskItemView>();
        private readonly List<ProgressMilestoneView> _progressMilestoneViews =
            new List<ProgressMilestoneView>();
        private readonly List<FoodRecipeItemView> _foodRecipeViews =
            new List<FoodRecipeItemView>();
        private QuestToastView _toastView;
        private Sequence _tabTransition;
        private Sequence _rewardTransition;
        private Tween _dailyFillTween;
        private RectTransform _pendingProgressRewardSource;
        private GameObject _flyingStarReward;
        private int _activeTabIndex = -1;
        private bool _isConstructed;

        [Inject]
        public void Construct(
            IDailyQuestService dailyQuestService,
            IProgressQuestService progressQuestService,
            IFoodRecipeService foodRecipeService,
            IPublisher<QuestToastRequestedPayload> toastPublisher,
            ISubscriber<DailyQuestStateChangedPayload> stateSubscriber,
            ISubscriber<QuestRewardGrantedPayload> rewardSubscriber,
            ISubscriber<ProgressQuestStateChangedPayload> progressStateSubscriber,
            ISubscriber<ProgressRewardClaimedPayload> progressRewardSubscriber,
            ISubscriber<FoodRecipeStateChangedPayload> foodStateSubscriber,
            ISubscriber<QuestToastRequestedPayload> toastSubscriber)
        {
            if (_isConstructed) return;
            _isConstructed = true;
            _dailyQuestService = dailyQuestService;
            _progressQuestService = progressQuestService;
            _foodRecipeService = foodRecipeService;
            _toastPublisher = toastPublisher;
            _subscriptions.Add(stateSubscriber.Subscribe(_ => Render()));
            _subscriptions.Add(rewardSubscriber.Subscribe(OnRewardGranted));
            _subscriptions.Add(progressStateSubscriber.Subscribe(_ => RenderProgress()));
            _subscriptions.Add(progressRewardSubscriber.Subscribe(OnProgressRewardClaimed));
            _subscriptions.Add(foodStateSubscriber.Subscribe(_ => RenderFood()));
            _subscriptions.Add(toastSubscriber.Subscribe(ShowToast));
            _dailyQuestService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            _progressQuestService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            InitializeFoodAsync().Forget();
            Render();
            RenderProgress();
        }

        public void OnBeforeWindowOpen()
        {
            GameplayInputBlockRegistry.Add(this);
            RegisterButtons();
            KillTabTransition();
            _activeTabIndex = -1;
            ShowTab(0);
            ResetTaskScroll();
            _dailyQuestService?.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            _progressQuestService?.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            InitializeFoodAsync().Forget();
            Render();
            RenderProgress();
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
            KillMotion();
            _toastView?.HideImmediate();
        }

        private void RegisterButtons()
        {
            UnregisterButtons();
            _dailyTab?.onClick.AddListener(ShowDaily);
            _progressTab?.onClick.AddListener(ShowProgress);
            _foodTab?.onClick.AddListener(ShowFood);
            _closeButton?.onClick.AddListener(Close);
        }

        private void UnregisterButtons()
        {
            _dailyTab?.onClick.RemoveListener(ShowDaily);
            _progressTab?.onClick.RemoveListener(ShowProgress);
            _foodTab?.onClick.RemoveListener(ShowFood);
            _closeButton?.onClick.RemoveListener(Close);
        }

        private void ShowTab(int index)
        {
            index = Mathf.Clamp(index, 0, 2);
            if (_activeTabIndex == index) return;

            int previousIndex = _activeTabIndex;
            GameObject previousPanel = GetTabPanel(previousIndex);
            GameObject nextPanel = GetTabPanel(index);
            _activeTabIndex = index;

            UpdateTabVisuals(index);
            if (index == 0) Render();
            if (index == 1) RenderProgress();
            if (index == 2) RenderFood();

            if (previousIndex < 0 || previousPanel == null || nextPanel == null)
            {
                SetOnlyTabActive(index);
                return;
            }

            PlayTabTransition(
                previousPanel,
                nextPanel,
                index > previousIndex ? 1 : -1);
        }

        private void ShowDaily()
        {
            ShowTab(0);
            ResetTaskScroll();
        }

        private void ShowProgress()
        {
            ShowTab(1);
            ResetProgressScroll();
        }
        private void ShowFood() => ShowTab(2);

        private async UniTaskVoid InitializeFoodAsync()
        {
            if (_foodRecipeService == null) return;
            await _foodRecipeService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy());
            RenderFood();
        }

        private GameObject GetTabPanel(int index)
        {
            return index switch
            {
                0 => _dailyPanel,
                1 => _progressPlaceholder,
                2 => _foodPlaceholder,
                _ => null
            };
        }

        private void SetOnlyTabActive(int index)
        {
            SetPanelState(_dailyPanel, index == 0);
            SetPanelState(_progressPlaceholder, index == 1);
            SetPanelState(_foodPlaceholder, index == 2);
        }

        private static void SetPanelState(GameObject panel, bool active)
        {
            if (panel == null) return;
            panel.SetActive(active);

            RectTransform rect = panel.transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = Vector2.zero;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
        }

        private void PlayTabTransition(
            GameObject previousPanel,
            GameObject nextPanel,
            int direction)
        {
            KillTabTransition();
            SetOnlyTabActive(_activeTabIndex);

            previousPanel.SetActive(true);
            nextPanel.SetActive(true);

            RectTransform previousRect =
                previousPanel.transform as RectTransform;
            RectTransform nextRect = nextPanel.transform as RectTransform;
            if (previousRect == null || nextRect == null)
            {
                SetOnlyTabActive(_activeTabIndex);
                return;
            }

            CanvasGroup previousGroup = GetOrAddCanvasGroup(previousPanel);
            CanvasGroup nextGroup = GetOrAddCanvasGroup(nextPanel);
            Vector2 previousRest = previousRect.anchoredPosition;
            Vector2 nextRest = nextRect.anchoredPosition;
            float offset = 18f * Mathf.Sign(direction);

            previousGroup.alpha = 1f;
            nextGroup.alpha = 0f;
            nextRect.anchoredPosition =
                nextRest + Vector2.right * offset;

            _tabTransition = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _tabTransition.Insert(
                0f,
                previousRect
                    .DOAnchorPos(
                        previousRest - Vector2.right * offset,
                        0.07f)
                    .SetEase(Ease.InQuad));
            _tabTransition.Insert(
                0f,
                previousGroup
                    .DOFade(0f, 0.07f)
                    .SetEase(Ease.InQuad));
            _tabTransition.Insert(
                0.07f,
                nextRect
                    .DOAnchorPos(nextRest, 0.10f)
                    .SetEase(Ease.OutCubic));
            _tabTransition.Insert(
                0.07f,
                nextGroup
                    .DOFade(1f, 0.10f)
                    .SetEase(Ease.OutQuad));
            _tabTransition.OnComplete(() =>
            {
                previousRect.anchoredPosition = previousRest;
                previousGroup.alpha = 1f;
                previousPanel.SetActive(false);
                nextRect.anchoredPosition = nextRest;
                nextGroup.alpha = 1f;
                _tabTransition = null;
            });
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private void KillTabTransition()
        {
            _tabTransition?.Kill(false);
            _tabTransition = null;
            if (_activeTabIndex >= 0)
                SetOnlyTabActive(_activeTabIndex);
        }

        private void Render()
        {
            if (_dailyQuestService == null) return;
            DailyQuestViewState state = _dailyQuestService.GetViewState();
            if (_lockedReason != null)
            {
                _lockedReason.gameObject.SetActive(!state.IsReady);
                _lockedReason.text = state.LockedReason ?? string.Empty;
            }

            if (_countdown != null)
                _countdown.text = FormatCountdown(state.TimeUntilReset);
            if (_totalPoints != null)
                _totalPoints.text = $"{state.TotalPoints} ĐIỂM";
            UpdateDailyMilestoneFill(state);

            int taskCount = state.Tasks?.Count ?? 0;
            EnsureTaskViews(taskCount);
            for (int i = 0; i < _taskViews.Count; i++)
            {
                DailyQuestTaskViewData task =
                    state.Tasks != null && i < state.Tasks.Count
                        ? state.Tasks[i]
                        : null;
                _taskViews[i]?.Bind(task);
            }

            for (int i = 0; i < (_milestones?.Length ?? 0); i++)
            {
                DailyMilestoneViewData milestone =
                    state.Milestones != null && i < state.Milestones.Count
                        ? state.Milestones[i]
                        : null;
                _milestones[i]?.Bind(milestone, ClaimMilestone);
            }
        }

        private void EnsureTaskViews(int count)
        {
            if (_taskTemplate == null || _taskContent == null) return;
            _taskTemplate.gameObject.SetActive(false);

            while (_taskViews.Count < count)
            {
                QuestTaskItemView view = Instantiate(_taskTemplate, _taskContent);
                view.name = $"Task {_taskViews.Count + 1}";
                view.gameObject.SetActive(true);
                _taskViews.Add(view);
            }

            for (int i = 0; i < _taskViews.Count; i++)
                _taskViews[i].gameObject.SetActive(i < count);
        }

        private void ResetTaskScroll()
        {
            if (_taskScroll == null) return;
            Canvas.ForceUpdateCanvases();
            _taskScroll.StopMovement();
            _taskScroll.verticalNormalizedPosition = 1f;
        }

        private void ClaimMilestone(string milestoneId)
        {
            _dailyQuestService.ClaimMilestoneAsync(
                milestoneId,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void UpdateDailyMilestoneFill(DailyQuestViewState state)
        {
            if (_dailyMilestoneFill == null) return;

            int finalMilestone = 0;
            if (state?.Milestones != null)
            {
                for (int i = 0; i < state.Milestones.Count; i++)
                {
                    DailyMilestoneViewData milestone = state.Milestones[i];
                    if (milestone != null)
                        finalMilestone = Mathf.Max(
                            finalMilestone, milestone.RequiredPoints);
                }
            }

            float targetFill = finalMilestone > 0
                ? Mathf.Clamp01((float)state.TotalPoints / finalMilestone)
                : 0f;

            _dailyFillTween?.Kill(false);
            _dailyFillTween = _dailyMilestoneFill
                .DOFillAmount(targetFill, 0.35f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void UpdateTabVisuals(int activeIndex)
        {
            ApplyTabVisual(
                _dailyTabVisual, _dailyTabLabel, activeIndex == 0);
            ApplyTabVisual(
                _progressTabVisual, _progressTabLabel, activeIndex == 1);
            ApplyTabVisual(
                _foodTabVisual, _foodTabLabel, activeIndex == 2);
        }

        private void ApplyTabVisual(
            Image visual,
            TMP_Text label,
            bool selected)
        {
            if (visual != null)
            {
                visual.sprite = selected
                    ? _selectedTabSprite
                    : _inactiveTabSprite;
                visual.color = Color.white;
            }

            if (label != null)
                label.color = selected
                    ? _selectedTabTextColor
                    : _inactiveTabTextColor;
        }

        private void RenderProgress()
        {
            if (_progressQuestService == null) return;
            ProgressQuestViewState state = _progressQuestService.GetViewState();
            if (_progressStars != null)
                _progressStars.text = FormatNumber(state.Stars);

            int milestoneCount = state.Milestones?.Count ?? 0;
            EnsureProgressMilestoneViews(milestoneCount);
            for (int i = 0; i < _progressMilestoneViews.Count; i++)
            {
                ProgressMilestoneViewData milestone =
                    state.Milestones != null && i < state.Milestones.Count
                        ? state.Milestones[i]
                        : null;
                _progressMilestoneViews[i]?.Bind(
                    milestone, ClaimProgressMilestone);
            }

            if (_progressContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_progressContent);
                if (_progressPlaceholder == null ||
                    _progressPlaceholder.activeInHierarchy)
                    ConfigureProgressOverlays(milestoneCount);
            }
        }

        private void EnsureProgressMilestoneViews(int count)
        {
            if (_progressMilestoneTemplate == null || _progressContent == null)
                return;

            _progressMilestoneTemplate.gameObject.SetActive(false);
            while (_progressMilestoneViews.Count < count)
            {
                ProgressMilestoneView view = Instantiate(
                    _progressMilestoneTemplate, _progressContent);
                view.name =
                    $"Progress Milestone {_progressMilestoneViews.Count + 1}";
                view.gameObject.SetActive(true);
                _progressMilestoneViews.Add(view);
            }

            for (int i = 0; i < _progressMilestoneViews.Count; i++)
                _progressMilestoneViews[i].gameObject.SetActive(i < count);

            ConfigureProgressBambooTrack(count);
        }

        private void ConfigureProgressBambooTrack(int count)
        {
            RectTransform templateRect =
                _progressMilestoneTemplate.transform as RectTransform;
            HorizontalLayoutGroup layout =
                _progressContent.GetComponent<HorizontalLayoutGroup>();
            if (templateRect == null || layout == null) return;

            float itemPitch = templateRect.rect.width + layout.spacing;
            for (int i = 0; i < _progressMilestoneViews.Count; i++)
            {
                _progressMilestoneViews[i]?.ConfigureBambooTrack(
                    i == 0,
                    count,
                    itemPitch);
                _progressMilestoneViews[i]?.ConfigureBoardPosition(
                    GetProgressBoardOffset(i));
            }
        }

        private float GetProgressBoardOffset(int index)
        {
            return Mathf.Abs(index % 3) switch
            {
                1 => _progressBoardCycleOffsets.y,
                2 => _progressBoardCycleOffsets.z,
                _ => _progressBoardCycleOffsets.x
            };
        }

        private void ConfigureProgressOverlays(int count)
        {
            if (_progressContent == null || _progressLockOverlay == null)
                return;

            if (count > 0 && _progressMilestoneViews.Count > 0)
            {
                _progressMilestoneViews[0]?.AttachBambooOverlay(
                    _progressContent);
            }

            _progressLockOverlay.SetAsLastSibling();

            for (int i = 0; i < _progressMilestoneViews.Count; i++)
            {
                ProgressMilestoneView view = _progressMilestoneViews[i];
                if (view == null) continue;

                if (i < count)
                    view.AttachLockOverlay(_progressLockOverlay);
                else
                    view.HideDetachedLock();
            }
        }

        private void ResetProgressScroll()
        {
            if (_progressScroll == null) return;
            Canvas.ForceUpdateCanvases();
            if (_progressContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_progressContent);
                ConfigureProgressOverlays(
                    CountActiveProgressMilestones());
            }
            _progressScroll.StopMovement();
            _progressScroll.horizontalNormalizedPosition = 0f;
        }

        private int CountActiveProgressMilestones()
        {
            int count = 0;
            for (int i = 0; i < _progressMilestoneViews.Count; i++)
            {
                ProgressMilestoneView view = _progressMilestoneViews[i];
                if (view != null && view.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private void ClaimProgressMilestone(string milestoneId)
        {
            _pendingProgressRewardSource = null;
            for (int i = 0; i < _progressMilestoneViews.Count; i++)
            {
                ProgressMilestoneView view = _progressMilestoneViews[i];
                if (view == null || view.MilestoneId != milestoneId) continue;

                _pendingProgressRewardSource = view.RewardAnchor;
                view.PlayClaimFeedback();
                break;
            }

            _progressQuestService.ClaimMilestoneAsync(
                milestoneId,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void RenderFood()
        {
            if (_foodRecipeService == null || _foodPlaceholder == null) return;
            FoodRecipeViewState state = _foodRecipeService.GetViewState();
            int recipeCount = state.Recipes?.Count ?? 0;
            EnsureFoodRecipeViews(recipeCount);

            for (int i = 0; i < _foodRecipeViews.Count; i++)
            {
                FoodRecipeViewData recipe =
                    state.Recipes != null && i < state.Recipes.Count
                        ? state.Recipes[i]
                        : null;
                _foodRecipeViews[i]?.Bind(
                    recipe,
                    state.LockIcon,
                    _progressStarIcon != null
                        ? _progressStarIcon.sprite
                        : null,
                    RequestUnlockRecipe,
                    RequestCookRecipe);
            }
        }

        private void EnsureFoodRecipeViews(int count)
        {
            RectTransform parent = _foodPlaceholder.transform as RectTransform;
            if (parent == null) return;

            Graphic legacyFullMock = _foodPlaceholder.GetComponent<Graphic>();
            if (legacyFullMock != null) legacyFullMock.enabled = false;

            DisableLegacyFoodMock("Top Recipe");
            DisableLegacyFoodMock("Top Explore");
            DisableLegacyFoodMock("Bottom Recipe");
            DisableLegacyFoodMock("Bottom Explore");

            while (_foodRecipeViews.Count < count)
            {
                FoodRecipeItemView view = FoodRecipeItemView.Create(
                    parent,
                    _foodTabLabel != null ? _foodTabLabel.font : null,
                    _foodRecipeViews.Count);
                _foodRecipeViews.Add(view);
            }

            for (int i = 0; i < _foodRecipeViews.Count; i++)
                _foodRecipeViews[i].gameObject.SetActive(i < count);
        }

        private void DisableLegacyFoodMock(string childName)
        {
            Transform child = _foodPlaceholder.transform.Find(childName);
            if (child != null) child.gameObject.SetActive(false);
        }

        private void RequestUnlockRecipe(string recipeId)
        {
            UnlockRecipeAsync(recipeId).Forget();
        }

        private async UniTaskVoid UnlockRecipeAsync(string recipeId)
        {
            if (_foodRecipeService == null) return;
            FoodRecipeUnlockResult result =
                await _foodRecipeService.TryUnlockAsync(
                    recipeId,
                    this.GetCancellationTokenOnDestroy());

            switch (result.Code)
            {
                case FoodRecipeUnlockResultCode.Success:
                case FoodRecipeUnlockResultCode.AlreadyUnlocked:
                    RenderFood();
                    break;
                case FoodRecipeUnlockResultCode.PrerequisiteLocked:
                    RequestToast("Hãy mở món phía trên trước");
                    break;
                case FoodRecipeUnlockResultCode.InsufficientStars:
                    RequestToast(
                        $"Cần {result.RequiredStars} sao để mở khóa");
                    break;
                case FoodRecipeUnlockResultCode.Busy:
                    break;
                default:
                    RequestToast("Không thể mở khóa lúc này");
                    break;
            }
        }

        private void RequestCookRecipe(string _)
        {
            RequestToast("Tính năng nấu ăn đang được phát triển");
        }

        private void RequestToast(
            string message,
            QuestToastStyle style = QuestToastStyle.Info)
        {
            _toastPublisher?.Publish(
                new QuestToastRequestedPayload(message, style));
        }

        private void ShowToast(QuestToastRequestedPayload payload)
        {
            EnsureToastView()?.Show(payload);
        }

        private QuestToastView EnsureToastView()
        {
            if (_toastView != null) return _toastView;
            if (_rewardToast == null) return null;

            _toastView = _rewardToast.GetComponent<QuestToastView>();
            if (_toastView == null)
                _toastView = _rewardToast.AddComponent<QuestToastView>();
            _toastView.Configure(_rewardToastText);
            return _toastView;
        }

        private void OnRewardGranted(QuestRewardGrantedPayload payload)
        {
            if (payload.ReconciledAtStartup || _rewardToast == null) return;
            RequestToast($"+{payload.Coins}", QuestToastStyle.Success);
        }

        private void OnProgressRewardClaimed(ProgressRewardClaimedPayload payload)
        {
            PlayProgressStarReward();
        }

        private void PlayProgressStarReward()
        {
            if (!gameObject.activeInHierarchy ||
                _progressStarIcon == null ||
                _pendingProgressRewardSource == null)
            {
                _pendingProgressRewardSource = null;
                return;
            }

            if (_flyingStarReward != null)
                Destroy(_flyingStarReward);

            _flyingStarReward = new GameObject(
                "Flying Star Reward",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform flyingRect =
                _flyingStarReward.GetComponent<RectTransform>();
            Image flyingImage = _flyingStarReward.GetComponent<Image>();
            flyingRect.SetParent(transform, false);
            flyingRect.position = _pendingProgressRewardSource.position;
            flyingRect.sizeDelta = _progressStarIcon.rectTransform.rect.size;
            flyingRect.localScale = Vector3.one;
            flyingImage.sprite = _progressStarIcon.sprite;
            flyingImage.preserveAspect = true;
            flyingImage.raycastTarget = false;

            Vector3 start = flyingRect.position;
            Vector3 end = _progressStarIcon.rectTransform.position;
            float peakY = Mathf.Max(start.y, end.y) + 70f;

            _rewardTransition?.Kill(false);
            _rewardTransition = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _rewardTransition.Insert(
                0f,
                flyingRect
                    .DOMoveX(end.x, 0.45f)
                    .SetEase(Ease.InOutCubic));
            _rewardTransition.Insert(
                0f,
                flyingRect
                    .DOMoveY(peakY, 0.20f)
                    .SetEase(Ease.OutQuad));
            _rewardTransition.Insert(
                0.20f,
                flyingRect
                    .DOMoveY(end.y, 0.25f)
                    .SetEase(Ease.InQuad));
            _rewardTransition.Insert(
                0f,
                flyingRect
                    .DOScale(1.12f, 0.12f)
                    .SetEase(Ease.OutCubic));
            _rewardTransition.Insert(
                0.12f,
                flyingRect
                    .DOScale(0.55f, 0.33f)
                    .SetEase(Ease.InCubic));
            _rewardTransition.OnComplete(() =>
            {
                if (_flyingStarReward != null)
                    Destroy(_flyingStarReward);
                _flyingStarReward = null;

                RectTransform starRect =
                    _progressStarIcon.rectTransform;
                starRect.DOKill(false);
                starRect
                    .DOPunchScale(
                        Vector3.one * 0.10f,
                        0.18f,
                        4,
                        0.35f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                _rewardTransition = null;
            });

            _pendingProgressRewardSource = null;
        }

        private void KillMotion()
        {
            KillTabTransition();
            _rewardTransition?.Kill(false);
            _rewardTransition = null;
            _dailyFillTween?.Kill(false);
            _dailyFillTween = null;
            _pendingProgressRewardSource = null;

            if (_flyingStarReward != null)
                Destroy(_flyingStarReward);
            _flyingStarReward = null;

            if (_rewardToast != null)
            {
                RectTransform toastRect =
                    _rewardToast.transform as RectTransform;
                if (toastRect != null) toastRect.localScale = Vector3.one;
                CanvasGroup toastGroup =
                    _rewardToast.GetComponent<CanvasGroup>();
                if (toastGroup != null) toastGroup.alpha = 1f;
            }
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            int hours = Mathf.Max(0, (int)remaining.TotalHours);
            return $"{hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        }

        protected override void OnDestroy()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
            KillMotion();
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            base.OnDestroy();
        }
    }
}
