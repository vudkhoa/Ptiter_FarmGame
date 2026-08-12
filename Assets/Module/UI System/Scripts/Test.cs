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
    private ISubscriber<MapPlacementStoppedPayload> _placementStoppedSub;
    private ObjectDatabaseSO _database;

    // Gọi Show Trong Awake không đảm bảo thứ tự và chưa khởi tại xong nên dễ lỗi chỗ UICollections.
    [Inject]
    public void Construct(
        MapService map,
        ObjectDatabaseSO database,
        ISubscriber<ClockTickPayload> tickSub,
        ISubscriber<MapPlacementStoppedPayload> placementStoppedSub)
    {
        _map = map;
        _database = database;
        _tickSub = tickSub;
        _placementStoppedSub = placementStoppedSub;
    }

    private void Start()
    {
        _windowsManager.Open(_choosenWindow);

        if (_windowsManager.TryGetWindowInstance(_choosenWindow, out SelectObjectsScreen screen))
            screen.Bind(_map, _database, _tickSub, _placementStoppedSub);
    }
}
