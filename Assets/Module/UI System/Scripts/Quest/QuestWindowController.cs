using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
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

        [Header("Reward feedback")]
        [SerializeField] private GameObject _rewardToast;
        [SerializeField] private TMP_Text _rewardToastText;

        private IDailyQuestService _dailyQuestService;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<QuestTaskItemView> _taskViews =
            new List<QuestTaskItemView>();
        private Coroutine _toastRoutine;
        private bool _isConstructed;

        [Inject]
        public void Construct(
            IDailyQuestService dailyQuestService,
            ISubscriber<DailyQuestStateChangedPayload> stateSubscriber,
            ISubscriber<QuestRewardGrantedPayload> rewardSubscriber)
        {
            if (_isConstructed) return;
            _isConstructed = true;
            _dailyQuestService = dailyQuestService;
            _subscriptions.Add(stateSubscriber.Subscribe(_ => Render()));
            _subscriptions.Add(rewardSubscriber.Subscribe(OnRewardGranted));
            _dailyQuestService.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            Render();
        }

        public void OnBeforeWindowOpen()
        {
            RegisterButtons();
            ShowTab(0);
            ResetTaskScroll();
            _dailyQuestService?.EnsureInitializedAsync(
                this.GetCancellationTokenOnDestroy()).Forget();
            Render();
        }

        public void OnWindowClosed()
        {
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
            if (index == 0) Render();
        }

        private void ShowDaily()
        {
            ShowTab(0);
            ResetTaskScroll();
        }

        private void ShowProgress() => ShowTab(1);
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

        private void OnRewardGranted(QuestRewardGrantedPayload payload)
        {
            if (payload.ReconciledAtStartup || _rewardToast == null) return;
            if (_rewardToastText != null)
                _rewardToastText.text = $"+{payload.Coins}";
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

        protected override void OnDestroy()
        {
            UnregisterButtons();
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            base.OnDestroy();
        }
    }
}
