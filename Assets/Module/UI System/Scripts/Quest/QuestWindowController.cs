using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using Core.Module.Quest;
using Cysharp.Threading.Tasks;
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
        private Coroutine _toastRoutine;
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
            if (_dailyPanel != null) _dailyPanel.SetActive(index == 0);
            if (_progressPlaceholder != null) _progressPlaceholder.SetActive(index == 1);
            if (_foodPlaceholder != null) _foodPlaceholder.SetActive(index == 2);
            UpdateTabVisuals(index);
            if (index == 0) Render();
            if (index == 1) RenderProgress();
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

            _dailyMilestoneFill.fillAmount = finalMilestone > 0
                ? Mathf.Clamp01((float)state.TotalPoints / finalMilestone)
                : 0f;
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
            _progressQuestService.ClaimMilestoneAsync(
                milestoneId,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnRewardGranted(QuestRewardGrantedPayload payload)
        {
            if (payload.ReconciledAtStartup || _rewardToast == null) return;
            if (_rewardToastText != null)
                _rewardToastText.text = $"+{payload.Coins}";
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ShowRewardToast());
        }

        private void OnProgressRewardClaimed(ProgressRewardClaimedPayload payload)
        {
            ShowRewardToast($"+{payload.Stars} SAO");
        }

        private void ShowRewardToast(string text)
        {
            if (_rewardToast == null) return;
            if (_rewardToastText != null) _rewardToastText.text = text;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ShowRewardToast());
        }

        private IEnumerator ShowRewardToast()
        {
            _rewardToast.SetActive(true);
            yield return new WaitForSecondsRealtime(1.5f);
            _rewardToast.SetActive(false);
            _toastRoutine = null;
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
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            base.OnDestroy();
        }
    }
}
