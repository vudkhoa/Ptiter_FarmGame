using System;
using Core.Module.Farm;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyOwn.ServiceHarness
{
    [DisallowMultipleComponent]
    public sealed class FarmSeedSelectorItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _costText;

        private static readonly Color UnaffordableTint = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private FarmEntityData _entity;
        private Action<FarmEntityData> _onSelected;

        public void Bind(FarmEntityData entity, bool canAfford, Action<FarmEntityData> onSelected)
        {
            _button.onClick.RemoveListener(Select);
            _entity = entity;
            _onSelected = onSelected;

            _nameText.text = entity.entityName;
            _costText.text = $"{entity.coinCost}";

            _icon.sprite = entity.selectorIcon;
            _icon.preserveAspect = true;
            _icon.enabled = entity.selectorIcon != null;

            // Stays clickable even when it cannot be bought: a dead button never tells the player
            // why. The row only dims, and the selector answers the tap with a toast.
            _button.interactable = true;
            _background.color = canAfford ? Color.white : UnaffordableTint;

            _button.onClick.AddListener(Select);
        }

        private void Select() => _onSelected?.Invoke(_entity);

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Select);
        }
    }
}
