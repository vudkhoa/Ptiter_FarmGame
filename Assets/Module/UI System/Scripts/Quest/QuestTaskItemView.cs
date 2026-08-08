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
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Image _rewardBackground;
        [SerializeField] private Sprite _defaultRewardSprite;
        [SerializeField] private Sprite _completedRewardSprite;

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

            if (_title != null)
            {
                string taskText = !string.IsNullOrWhiteSpace(data.Description)
                    ? data.Description.Trim().TrimEnd('.')
                    : data.Title;
                _title.text = taskText;
            }

            if (_description != null)
                _description.gameObject.SetActive(false);

            if (_progress != null)
            {
                _progress.gameObject.SetActive(true);
                _progress.text = $"{data.CurrentAmount}/{data.RequiredAmount}";
            }

            if (_progressFill != null)
            {
                int requiredAmount = Mathf.Max(1, data.RequiredAmount);
                _progressFill.fillAmount = Mathf.Clamp01(
                    (float)data.CurrentAmount / requiredAmount);
            }

            if (_reward != null) _reward.text = data.CoinReward.ToString();
            if (_rewardBackground != null)
                _rewardBackground.sprite =
                    data.IsCompleted && _completedRewardSprite != null
                        ? _completedRewardSprite
                        : _defaultRewardSprite;
        }
    }
}
