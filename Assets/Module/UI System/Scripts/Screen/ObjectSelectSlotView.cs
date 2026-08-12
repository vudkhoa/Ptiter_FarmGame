using System;
using Core.Module.Map;
using Core.Module.Tutorial;
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

    private bool _isSoilAnchor;
    private int _displayedPlacedCount = -1;

    #region Properties
    public int ObjectId { get; private set; } = -1;
    #endregion

    #region Unity Lifecycle
    private void OnDestroy() => SetSoilAnchor(false);
    #endregion

    #region Public API
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

        _displayedPlacedCount = -1;
        SetSoilAnchor(data.FarmRole == FarmObjectRole.Soil);
    }

    // Guarded on the last value: the owning screen polls this from Update, and int.ToString
    // allocates a fresh string every call.
    public void SetPlacedCount(int count)
    {
        if (_placedCountLabel == null || _displayedPlacedCount == count) return;

        _displayedPlacedCount = count;
        _placedCountLabel.text = count.ToString();
    }

    public void Clear()
    {
        ObjectId = -1;
        _displayedPlacedCount = -1;
        SetSoilAnchor(false);
        gameObject.SetActive(false);
    }
    #endregion

    #region Private Methods
    /// Claimed from code, not authored on the prefab: rows are pooled and only learn which object
    /// they represent at Bind time, so a prefab id would tag whichever row happened to be first.
    private void SetSoilAnchor(bool isSoil)
    {
        if (isSoil == _isSoilAnchor) return;

        _isSoilAnchor = isSoil;
        if (isSoil)
            TutorialAnchorRegistry.Register(TutorialAnchorIds.SoilButton, transform);
        else
            TutorialAnchorRegistry.Unregister(TutorialAnchorIds.SoilButton, transform);
    }
    #endregion
}
