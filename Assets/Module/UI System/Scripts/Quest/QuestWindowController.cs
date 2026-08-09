using System;
using System.Collections.Generic;
using System.Globalization;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using Core.Module.Quest;
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

        [Header("Reward feedback")]
        [SerializeField] private GameObject _rewardToast;
        [SerializeField] private TMP_Text _rewardToastText;

        private IDailyQuestService _dailyQuestService;
        private IProgressQuestService _progressQuestService;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<QuestTaskItemView> _taskViews =
            new List<QuestTaskItemView>();
        private readonly List<ProgressMilestoneView> _progressMilestoneViews =
            new List<ProgressMilestoneView>();
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
            ISubscriber<DailyQuestStateChangedPayload> stateSubscriber,
            ISubscriber<QuestRewardGrantedPayload> rewardSubscriber,
            ISubscriber<ProgressQuestStateChangedPayload> progressStateSubscriber,
            ISubscriber<ProgressRewardClaimedPayload> progressRewardSubscriber)
        {
            if (_isConstructed) return;
            _isConstructed = true;
            _dailyQuestService = dailyQuestService;
            _progressQuestService = progressQuestService;
            _subscriptions.Add(stateSubscriber.Subscribe(_ => Render()));
            _subscriptions.Add(rewardSubscriber.Subscribe(OnRewardGranted));
            _subscriptions.Add(progressStateSubscriber.Subscribe(_ => RenderProgress()));
            _subscriptions.Add(progressRewardSubscriber.Subscribe(OnProgressRewardClaimed));
            _dailyQuestService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            _progressQuestService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
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
            Render();
            RenderProgress();
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
            KillMotion();
            if (_rewardToast != null) _rewardToast.SetActive(false);
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
                LayoutRebuilder.ForceRebuildLayoutImmediate(_progressContent);
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
        }

        private void ResetProgressScroll()
        {
            if (_progressScroll == null) return;
            Canvas.ForceUpdateCanvases();
            if (_progressContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_progressContent);
            _progressScroll.StopMovement();
            _progressScroll.horizontalNormalizedPosition = 0f;
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

        private void OnRewardGranted(QuestRewardGrantedPayload payload)
        {
            if (payload.ReconciledAtStartup || _rewardToast == null) return;
            ShowRewardToast($"+{payload.Coins}");
        }

        private void OnProgressRewardClaimed(ProgressRewardClaimedPayload payload)
        {
            PlayProgressStarReward();
        }

        private void ShowRewardToast(string text)
        {
            if (_rewardToast == null) return;
            if (_rewardToastText != null) _rewardToastText.text = text;

            _rewardTransition?.Kill(false);
            _rewardToast.SetActive(true);
            RectTransform toastRect =
                _rewardToast.transform as RectTransform;
            CanvasGroup toastGroup = GetOrAddCanvasGroup(_rewardToast);
            if (toastRect == null) return;

            toastRect.localScale = Vector3.one * 0.92f;
            toastGroup.alpha = 0f;
            _rewardTransition = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _rewardTransition.Join(
                toastRect
                    .DOScale(1f, 0.14f)
                    .SetEase(Ease.OutCubic));
            _rewardTransition.Join(
                toastGroup
                    .DOFade(1f, 0.10f)
                    .SetEase(Ease.OutQuad));
            _rewardTransition.AppendInterval(0.75f);
            _rewardTransition.Append(
                toastGroup
                    .DOFade(0f, 0.15f)
                    .SetEase(Ease.InQuad));
            _rewardTransition.OnComplete(() =>
            {
                _rewardToast.SetActive(false);
                toastRect.localScale = Vector3.one;
                toastGroup.alpha = 1f;
                _rewardTransition = null;
            });
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
