using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Core.Module.Currency
{
    public sealed class CurrencyHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _balanceLabel;

        public void SetBalance(int balance)
        {
            if (_balanceLabel == null) return;
            _balanceLabel.text = Math.Max(0, balance).ToString(
                "N0",
                CultureInfo.InvariantCulture);
        }
    }
}
