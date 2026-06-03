using BrunoMikoski.UIManager;
using Core.Module.Map;
using UnityEngine;
using VContainer;

public class Test : MonoBehaviour
{
    [SerializeField] private WindowsManager _windowsManager;
    [SerializeField] private UIWindow _choosenWindow;

    [SerializeField] private MapService _map;

    [Inject]
    public void Construct(MapService map)
    {
        _map = map;
        ShowUI();
    }

    private void ShowUI()
    {
        _windowsManager.Open(_choosenWindow);

        if (_windowsManager.TryGetWindowInstance(_choosenWindow, out SelectObjectsScreen screen))
            screen.Bind(_map);
    }
}
