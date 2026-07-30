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
        private const int TasksPerPage = 2;

        [Header("Navigation")]
        [SerializeField] private Button _dailyTab;
        [SerializeField] private Button _progressTab;
        [SerializeField] private Button _foodTab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _previousPage;
        [SerializeField] private Button _nextPage;

        [Header("Panels")]
        [SerializeField] private GameObject _dailyPanel;
        [SerializeField] private GameObject _progressPlaceholder;
        [SerializeField] private GameObject _foodPlaceholder;

        [Header("Daily")]
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private TMP_Text _pageLabel;
        [SerializeField] private TMP_Text _totalPoints;
        [SerializeField] private TMP_Text _lockedReason;
        [SerializeField] private QuestTaskItemView[] _taskSlots;
        [SerializeField] private QuestMilestoneView[] _milestones;

        [Header("Reward feedback")]
        [SerializeField] private GameObject _rewardToast;
        [SerializeField] private TMP_Text _rewardToastText;

        private IDailyQuestService _dailyQuestService;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private int _pageIndex;
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
            _pageIndex = 0;
            RegisterButtons();
            ShowTab(0);
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
            _previousPage?.onClick.AddListener(PreviousPage);
            _nextPage?.onClick.AddListener(NextPage);
        }

        private void UnregisterButtons()
        {
            _dailyTab?.onClick.RemoveListener(ShowDaily);
            _progressTab?.onClick.RemoveListener(ShowProgress);
            _foodTab?.onClick.RemoveListener(ShowFood);
            _closeButton?.onClick.RemoveListener(Close);
            _previousPage?.onClick.RemoveListener(PreviousPage);
            _nextPage?.onClick.RemoveListener(NextPage);
        }

        private void ShowTab(int index)
        {
            if (_dailyPanel != null) _dailyPanel.SetActive(index == 0);
            if (_progressPlaceholder != null) _progressPlaceholder.SetActive(index == 1);
            if (_foodPlaceholder != null) _foodPlaceholder.SetActive(index == 2);
            if (index == 0) Render();
        }

        private void ShowDaily() => ShowTab(0);
        private void ShowProgress() => ShowTab(1);
        private void ShowFood() => ShowTab(2);

        private void PreviousPage()
        {
            _pageIndex = Mathf.Max(0, _pageIndex - 1);
            Render();
        }

        private void NextPage()
        {
            DailyQuestViewState state = _dailyQuestService?.GetViewState();
            int pageCount = GetPageCount(state?.Tasks?.Count ?? 0);
            _pageIndex = Mathf.Min(Mathf.Max(0, pageCount - 1), _pageIndex + 1);
            Render();
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

            int taskCount = state.Tasks?.Count ?? 0;
            int pageCount = GetPageCount(taskCount);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, pageCount - 1));
            if (_pageLabel != null)
                _pageLabel.text = $"{_pageIndex + 1}/{pageCount}";
            if (_countdown != null)
                _countdown.text = FormatCountdown(state.TimeUntilReset);
            if (_totalPoints != null)
                _totalPoints.text = state.TotalPoints.ToString();

            for (int slot = 0; slot < (_taskSlots?.Length ?? 0); slot++)
            {
                int taskIndex = _pageIndex * TasksPerPage + slot;
                DailyQuestTaskViewData task =
                    state.Tasks != null && taskIndex < state.Tasks.Count
                        ? state.Tasks[taskIndex]
                        : null;
                _taskSlots[slot]?.Bind(task);
            }

            for (int i = 0; i < (_milestones?.Length ?? 0); i++)
            {
                DailyMilestoneViewData milestone =
                    state.Milestones != null && i < state.Milestones.Count
                        ? state.Milestones[i]
                        : null;
                _milestones[i]?.Bind(milestone, ClaimMilestone);
            }

            if (_previousPage != null) _previousPage.interactable = _pageIndex > 0;
            if (_nextPage != null) _nextPage.interactable = _pageIndex + 1 < pageCount;
        }

        private void ClaimMilestone(string milestoneId)
        {
            _dailyQuestService.ClaimMilestoneAsync(
                milestoneId,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnRewardGranted(QuestRewardGrantedPayload payload)
        {
            // Startup reconcile is silent; normal same-session rewards get immediate feedback.
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

        private static int GetPageCount(int taskCount)
        {
            return Mathf.Max(1, Mathf.CeilToInt(taskCount / (float)TasksPerPage));
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
