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
        [SerializeField] private Image _statusBackground;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Image _giftImage;
        [SerializeField] private Sprite _lockedGiftSprite;
        [SerializeField] private Sprite _claimableGiftSprite;
        [SerializeField] private Sprite _claimedGiftSprite;

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
            if (_points != null) _points.text = $"MỐC {data.RequiredPoints}";
            if (_reward != null) _reward.text = $"+{data.CoinReward}";
            bool claimable = data.ClaimState == DailyMilestoneClaimState.Claimable;
            if (_claimButton != null)
            {
                _claimButton.interactable = claimable;
                _claimButton.onClick.AddListener(OnClaimClicked);
            }
            UpdateGift(data.ClaimState);
            UpdateStatus(data.ClaimState);
        }

        private void UpdateStatus(DailyMilestoneClaimState state)
        {
            string label;
            Color color;
            switch (state)
            {
                case DailyMilestoneClaimState.Claimable:
                    label = "NHẬN";
                    color = new Color(0.20f, 0.52f, 0.20f, 0.96f);
                    break;
                case DailyMilestoneClaimState.ClaimPending:
                    label = "ĐANG NHẬN";
                    color = new Color(0.78f, 0.49f, 0.12f, 0.96f);
                    break;
                case DailyMilestoneClaimState.Claimed:
                    label = "ĐÃ NHẬN";
                    color = new Color(0.28f, 0.42f, 0.20f, 0.96f);
                    break;
                default:
                    label = "CHƯA ĐỦ";
                    color = new Color(0.45f, 0.38f, 0.29f, 0.92f);
                    break;
            }

            if (_status != null) _status.text = label;
            if (_statusBackground != null) _statusBackground.color = color;
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

        private void UpdateGift(DailyMilestoneClaimState state)
        {
            if (_giftImage == null) return;

            Sprite stateSprite;
            switch (state)
            {
                case DailyMilestoneClaimState.Claimed:
                    stateSprite = _claimedGiftSprite;
                    break;
                case DailyMilestoneClaimState.Locked:
                    stateSprite = _lockedGiftSprite;
                    break;
                default:
                    stateSprite = _claimableGiftSprite;
                    break;
            }

            _giftImage.sprite = stateSprite != null
                ? stateSprite
                : _claimableGiftSprite;
            _giftImage.enabled = _giftImage.sprite != null;
        }
    }
}
