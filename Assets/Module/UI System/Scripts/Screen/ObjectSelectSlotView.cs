using System;
using Core.Module.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ObjectSelectSlotView : MonoBehaviour
{
    [SerializeField] private MapPlacer _placer;
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _placedCountLabel;
    [SerializeField] private TMP_Text _priceLabel;

    public int ObjectId { get; private set; } = -1;

    public void Bind(ObjectData data, IMapService map, Action onPlacementStarted)
    {
        ObjectId = data.ID;
        gameObject.SetActive(true);

        if (_nameLabel != null) _nameLabel.text = data.name;
        if (_icon != null)
        {
            _icon.sprite = data.SelectionIcon;
            _icon.enabled = data.SelectionIcon != null;
        }
        if (_priceLabel != null) _priceLabel.text = data.CoinPrice.ToString();
        if (_placer != null) _placer.Bind(map, data.ID, onPlacementStarted);
    }

    public void SetPlacedCount(int count)
    {
        if (_placedCountLabel != null)
            _placedCountLabel.text = count.ToString();
    }

    public void Clear()
    {
        ObjectId = -1;
        gameObject.SetActive(false);
    }
}
