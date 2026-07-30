using System;
using Core.Module.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyOwn.ServiceHarness
{
    public sealed class QuestMilestoneView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _points;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _locked;
        [SerializeField] private GameObject _claimed;
        [SerializeField] private GameObject _pending;

        private string _milestoneId;
        private Action<string> _claim;

        public void Bind(DailyMilestoneViewData data, Action<string> claim)
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
            if (_points != null) _points.text = data.RequiredPoints.ToString();
            if (_reward != null) _reward.text = data.CoinReward.ToString();
            bool claimable = data.ClaimState == DailyMilestoneClaimState.Claimable;
            if (_claimButton != null)
            {
                _claimButton.interactable = claimable;
                _claimButton.onClick.AddListener(OnClaimClicked);
            }
            if (_locked != null)
                _locked.SetActive(data.ClaimState == DailyMilestoneClaimState.Locked);
            if (_claimed != null)
                _claimed.SetActive(data.ClaimState == DailyMilestoneClaimState.Claimed);
            if (_pending != null)
                _pending.SetActive(data.ClaimState == DailyMilestoneClaimState.ClaimPending);
        }

        private void OnClaimClicked()
        {
            if (!string.IsNullOrWhiteSpace(_milestoneId))
                _claim?.Invoke(_milestoneId);
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(OnClaimClicked);
        }
    }
}
