using Core.Module.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyOwn.ServiceHarness
{
    public sealed class QuestTaskItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Image _rewardBackground;
        [SerializeField] private Sprite _defaultRewardSprite;
        [SerializeField] private Sprite _completedRewardSprite;
        [SerializeField] private GameObject _completedMark;
        [SerializeField] private GameObject _pendingMark;

        public void Bind(DailyQuestTaskViewData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
            }
            if (_title != null) _title.text = data.Title;
            if (_description != null) _description.text = data.Description;
            if (_progress != null)
                _progress.text = $"{data.CurrentAmount}/{data.RequiredAmount}";
            if (_progressBar != null)
            {
                _progressBar.minValue = 0;
                _progressBar.maxValue = Mathf.Max(1, data.RequiredAmount);
                _progressBar.value = data.CurrentAmount;
            }
            if (_reward != null) _reward.text = data.CoinReward.ToString();
            if (_rewardBackground != null)
                _rewardBackground.sprite =
                    data.IsCompleted && _completedRewardSprite != null
                        ? _completedRewardSprite
                        : _defaultRewardSprite;
            if (_completedMark != null) _completedMark.SetActive(data.IsCompleted);
            if (_pendingMark != null) _pendingMark.SetActive(data.IsRewardPending);
        }
    }
}
