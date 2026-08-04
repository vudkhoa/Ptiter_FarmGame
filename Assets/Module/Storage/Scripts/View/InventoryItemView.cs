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
            _button?.onClick.RemoveListener(Select);
            _definition = definition;
            _onSelected = onSelected;

            bool visible = definition != null && amount > 0;
            gameObject.SetActive(visible);
            if (!visible) return;

            if (_icon != null)
            {
                _icon.sprite = definition.icon;
                _icon.enabled = definition.icon != null;
            }
            if (_amount != null) _amount.text = amount.ToString();
            if (_background != null)
                _background.sprite = selected && _selectedSprite != null
                    ? _selectedSprite
                    : _normalSprite;
            _button?.onClick.AddListener(Select);
        }

        private void Select() => _onSelected?.Invoke(_definition);

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(Select);
        }
    }
}
