using BrunoMikoski.UIManager;
using Core.Module.Map;
using Core.Module.Time;
using MessagePipe;
using System;
using UnityEngine;
using VContainer;

public class Test : MonoBehaviour
{
    [SerializeField] private WindowsManager _windowsManager;
    [SerializeField] private UIWindow _choosenWindow;

    [SerializeField] private MapService _map;

    private ISubscriber<ClockTickPayload> _tickSub;

    // Gọi Show Trong Awake không đảm bảo thứ tự và chưa khởi tại xong nên dễ lỗi chỗ UICollections.
    [Inject]
    public void Construct(MapService map, ISubscriber<ClockTickPayload> tickSub)
    {
        _map = map;
        _tickSub = tickSub;
    }

    private void Start()
    {
        _windowsManager.Open(_choosenWindow);

        if (_windowsManager.TryGetWindowInstance(_choosenWindow, out SelectObjectsScreen screen))
            screen.Bind(_map, _tickSub);
    }
}
