using System;
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
        [SerializeField] private Image _coinIcon;
        [SerializeField] private Image _starIcon;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _locked;

        [Header("Board States")]
        [SerializeField] private Sprite _lockedBoardSprite;
        [SerializeField] private Sprite _claimableBoardSprite;
        [SerializeField] private Sprite _claimedBoardSprite;
        [SerializeField] private Image _boardImage;
        [SerializeField] private Material _lockedMaterial;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _lockedColor =
            new Color(0.82f, 0.80f, 0.70f, 1f);
        [SerializeField] private Color _normalRequirementColor = Color.white;
        [SerializeField] private Color _normalRewardColor =
            new Color(1f, 0.78f, 0.12f, 1f);
        [SerializeField] private Color _lockedTextColor =
            new Color(0.55f, 0.53f, 0.47f, 1f);

        private string _milestoneId;
        private Action<string> _claim;
        private Tween _claimFeedback;

        public string MilestoneId => _milestoneId;
        public RectTransform RewardAnchor =>
            _starIcon != null ? _starIcon.rectTransform : transform as RectTransform;

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
            if (_boardImage != null &&
                (_claimFeedback == null || !_claimFeedback.IsActive()))
                transform.localScale = Vector3.one;
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

                // Each state now has its own authored artwork; do not tint the board.
                _boardImage.material = null;
                _boardImage.color = boardSprite != null
                    ? _normalColor
                    : locked ? _lockedColor : _normalColor;
            }
            if (_requirement != null)
                _requirement.color = locked
                    ? _lockedTextColor
                    : _normalRequirementColor;
            if (_currentProgress != null)
                _currentProgress.color = locked
                    ? _lockedTextColor
                    : _normalRequirementColor;
            if (_reward != null)
                _reward.color = locked
                    ? _lockedTextColor
                    : _normalRewardColor;
            if (_coinIcon != null)
            {
                _coinIcon.material = locked ? _lockedMaterial : null;
                _coinIcon.color = locked && _lockedMaterial == null
                    ? _lockedTextColor
                    : Color.white;
            }
            if (_starIcon != null)
            {
                _starIcon.material = locked ? _lockedMaterial : null;
                _starIcon.color = locked && _lockedMaterial == null
                    ? _lockedTextColor
                    : Color.white;
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
            target.localScale = Vector3.one;
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
        }
    }
}
