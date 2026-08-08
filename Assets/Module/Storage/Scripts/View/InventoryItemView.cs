using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Storage.View
{
    [DisallowMultipleComponent]
    public sealed class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private TMP_Text _fallbackLabel;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _selectedSprite;

        private InventoryItemDefinition _definition;
        private Action<InventoryItemDefinition> _onSelected;

        public void Bind(
            InventoryItemDefinition definition,
            int amount,
            bool selected,
            Action<InventoryItemDefinition> onSelected)
        {
            EnsureFallbackLabel();
            _button?.onClick.RemoveListener(Select);
            _definition = definition;
            _onSelected = onSelected;

            bool visible = definition != null && amount > 0;
            bool hasIcon = visible && definition.icon != null;
            gameObject.SetActive(true);

            if (_button != null)
                _button.interactable = visible;

            if (_icon != null)
            {
                _icon.sprite = visible ? definition.icon : null;
                _icon.enabled = hasIcon;
            }

            if (_fallbackLabel != null)
            {
                _fallbackLabel.text = visible && !hasIcon
                    ? definition.displayName
                    : string.Empty;
                _fallbackLabel.enabled = visible && !hasIcon;
            }

            if (_amount != null)
            {
                _amount.text = visible ? amount.ToString() : string.Empty;
                _amount.enabled = visible;
            }

            if (_background != null)
            {
                _background.enabled = true;
                _background.sprite = visible && selected && _selectedSprite != null
                    ? _selectedSprite
                    : _normalSprite;
            }

            if (visible)
                _button?.onClick.AddListener(Select);
        }

        private void EnsureFallbackLabel()
        {
            if (_fallbackLabel != null) return;

            GameObject labelObject = new GameObject(
                "Fallback Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 10f);
            rect.sizeDelta = new Vector2(78f, 62f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (_amount != null)
            {
                label.font = _amount.font;
                label.color = _amount.color;
            }
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.enabled = false;
            _fallbackLabel = label;
        }

        private void Select() => _onSelected?.Invoke(_definition);

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(Select);
        }
    }
}
