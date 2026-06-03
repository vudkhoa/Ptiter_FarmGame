using BrunoMikoski.UIManager;
using Core.Module.Map;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectObjectsScreen : WindowController
{
    [SerializeField] private Button _btnClose;
    [SerializeField] private Button _bgClose;

    [SerializeField] private List<MapPlacer> _buttons;

    public void Bind(MapService map)
    {
        if (_buttons == null) return;
        foreach (MapPlacer p in _buttons)
            if (p != null) p.Bind(map);
    }

    public void OnBeforeWindowOpen()
    {
        _btnClose?.onClick.AddListener(Close);
        _bgClose?.onClick.AddListener(Close);
    }

    public void OnAfterWindowClose()
    {
        _btnClose?.onClick.RemoveListener(Close);
        _bgClose?.onClick.RemoveListener(Close);
    }
}