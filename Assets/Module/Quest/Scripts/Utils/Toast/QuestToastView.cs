using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest.Utils
{
    [DisallowMultipleComponent]
    public sealed class QuestToastView : MonoBehaviour
    {
        private TMP_Text _label;
        private Image _background;
        private CanvasGroup _group;
        private RectTransform _rect;
        private Sequence _sequence;

        public void Configure(TMP_Text label)
        {
            _label = label;
            _background = GetComponent<Image>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _rect = transform as RectTransform;
            HideImmediate();
        }

        public void Show(QuestToastRequestedPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Message)) return;
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
            if (_background == null) _background = GetComponent<Image>();
            if (_group == null) _group = GetComponent<CanvasGroup>() ??
                                        gameObject.AddComponent<CanvasGroup>();
            if (_rect == null) _rect = transform as RectTransform;

            _sequence?.Kill(false);
            gameObject.SetActive(true);
            if (_label != null)
            {
                _label.text = payload.Message;
                _label.color = Color.white;
            }
            if (_background != null)
                _background.color = payload.Style == QuestToastStyle.Success
                    ? new Color(0.08f, 0.38f, 0.12f, 0.95f)
                    : new Color(0.03f, 0.03f, 0.03f, 0.94f);

            _group.alpha = 0f;
            if (_rect != null) _rect.localScale = Vector3.one * 0.94f;
            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _sequence.Append(_group.DOFade(1f, 0.14f));
            if (_rect != null)
                _sequence.Join(_rect.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
            _sequence.AppendInterval(Mathf.Max(0.4f, payload.Duration));
            _sequence.Append(_group.DOFade(0f, 0.18f));
            _sequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                _sequence = null;
            });
        }

        public void HideImmediate()
        {
            _sequence?.Kill(false);
            _sequence = null;
            if (_group != null) _group.alpha = 1f;
            if (_rect != null) _rect.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }
    }
}
