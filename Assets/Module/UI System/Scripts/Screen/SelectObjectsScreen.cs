using BrunoMikoski.UIManager;
using Core.Module.Input;
using Core.Module.Map;
using Core.Module.Time;
using Core.Module.Toast;
using Core.Module.Tutorial;
using MessagePipe;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class SelectObjectsScreen :
    WindowController,
    IOnBeforeWindowOpen,
    IOnWindowClosed
{
    [Header("Window")]
    [SerializeField] private Button _btnClose;
    [SerializeField] private Button _bgClose;
    [SerializeField] private ClockDisplay _display;

    [Header("Tabs")]
    [SerializeField] private Button _resourceTabButton;
    [SerializeField] private Image _resourceTabImage;
    [SerializeField] private Button _decorationTabButton;
    [SerializeField] private Image _decorationTabImage;
    [SerializeField] private Sprite _tabActiveSprite;
    [SerializeField] private Sprite _tabInactiveSprite;

    [Tooltip("Shown as a toast when a tab that is not implemented yet is tapped.")]
    [SerializeField] private string _lockedTabMessage = "Tính năng chưa mở!";

    [Header("Resource horizontal recycler")]
    [SerializeField] private ScrollRect _resourceRecycler;
    [SerializeField] private RectTransform _resourceContent;
    [SerializeField] private ObjectSelectSlotView _slotPrefab;

    private readonly List<ObjectData> _resourceObjects = new();
    private readonly List<ObjectSelectSlotView> _slotPool = new();
    private IMapService _map;
    private ObjectDatabaseSO _database;
    private IDisposable _placementStoppedSubscription;
    private int _displayedCountRevision = -1;
    private bool _reopenAfterPlacement;

    [Inject]
    public void Construct(
        IMapService map,
        ObjectDatabaseSO database,
        ISubscriber<ClockTickPayload> tickSub,
        ISubscriber<MapPlacementStoppedPayload> placementStoppedSub)
    {
        Bind(map, database, tickSub, placementStoppedSub);
    }

    public void Bind(
        IMapService map,
        ObjectDatabaseSO database,
        ISubscriber<ClockTickPayload> tickSub,
        ISubscriber<MapPlacementStoppedPayload> placementStoppedSub)
    {
        _map = map;
        _database = database;
        if (_display != null) _display.Subscriber(tickSub);

        _placementStoppedSubscription?.Dispose();
        _placementStoppedSubscription = placementStoppedSub?.Subscribe(OnPlacementStopped);

        // UIManager creates/opens the window before WindowOpenTrigger injects it.
        // Rebind the prefab-backed slots here so MapPlacer never keeps a null map.
        ReloadResourceItems();
    }

    public void OnBeforeWindowOpen()
    {
        _btnClose?.onClick.AddListener(Close);
        _resourceTabButton?.onClick.AddListener(ShowResources);
        _decorationTabButton?.onClick.AddListener(ShowLockedTabToast);
        GameplayInputBlockRegistry.Add(this);

        // Reported here rather than through an injected service: UIManager opens the window
        // before VContainer injects it, so the field would still be null.
        TutorialAnchorRegistry.Register(TutorialAnchorIds.ObjectsPanel, transform);
        TutorialSignalHub.Report(TutorialSignal.BuildMenuOpened);

        // Left interactable on purpose: the tab is locked, but the tap has to reach us so the
        // player is told why instead of pressing a dead button.
        if (_decorationTabButton != null)
            _decorationTabButton.interactable = true;

        ShowResources();
    }

    public void OnWindowClosed()
    {
        _btnClose?.onClick.RemoveListener(Close);
        _resourceTabButton?.onClick.RemoveListener(ShowResources);
        _decorationTabButton?.onClick.RemoveListener(ShowLockedTabToast);
        GameplayInputBlockRegistry.Remove(this);
        TutorialAnchorRegistry.Unregister(TutorialAnchorIds.ObjectsPanel, transform);
    }

    protected override void OnDestroy()
    {
        GameplayInputBlockRegistry.Remove(this);
        TutorialAnchorRegistry.Unregister(TutorialAnchorIds.ObjectsPanel, transform);
        _placementStoppedSubscription?.Dispose();
        base.OnDestroy();
    }

    private void Update()
    {
        RefreshPlacedCounts(force: false);
    }

    private void ShowResources()
    {
        ApplyTabVisual(_resourceTabImage, active: true);
        ApplyTabVisual(_decorationTabImage, active: false);
        if (_resourceRecycler != null) _resourceRecycler.gameObject.SetActive(true);

        ReloadResourceItems();
    }

    /// <summary>
    /// Raised through the hub rather than an injected service: UIManager opens this window before
    /// VContainer injects it, so a service field cannot be relied on from a click handler.
    /// The resource tab stays selected - a locked tab must not swap the panel out.
    /// </summary>
    private void ShowLockedTabToast()
    {
        ToastHub.Show(_lockedTabMessage, ToastStyle.Warning);
    }

    private void ReloadResourceItems()
    {
        if (_database == null || _resourceContent == null || _slotPrefab == null) return;

        _database.GetObjectsByMenuCategory(BuildMenuCategory.Resource, _resourceObjects);
        EnsurePoolSize(_resourceObjects.Count);

        for (int i = 0; i < _slotPool.Count; i++)
        {
            if (i < _resourceObjects.Count)
                _slotPool[i].Bind(_resourceObjects[i], _map, OnPlacementStarted);
            else
                _slotPool[i].Clear();
        }

        RefreshPlacedCounts(force: true);
    }

    private void EnsurePoolSize(int requiredCount)
    {
        while (_slotPool.Count < requiredCount)
        {
            ObjectSelectSlotView slot = Instantiate(_slotPrefab, _resourceContent);
            slot.name = $"Object Slot {_slotPool.Count}";
            _slotPool.Add(slot);
        }
    }

    private void ApplyTabVisual(Image image, bool active)
    {
        if (image == null) return;
        Sprite sprite = active ? _tabActiveSprite : _tabInactiveSprite;
        if (sprite != null) image.sprite = sprite;
    }

    private void OnPlacementStarted()
    {
        if (!IsOpen) return;

        _reopenAfterPlacement = true;
        Close();
    }

    private void OnPlacementStopped(MapPlacementStoppedPayload payload)
    {
        if (!_reopenAfterPlacement) return;

        // Cleared either way: the trip back to the map is over, so a later stop must not drag
        // this window up again out of nowhere.
        _reopenAfterPlacement = false;
        if (!payload.ReopenPicker) return;

        Open();
    }

    private void RefreshPlacedCounts(bool force)
    {
        if (_database == null) return;
        if (!force && _displayedCountRevision == _database.PlacedCountRevision) return;

        _displayedCountRevision = _database.PlacedCountRevision;
        foreach (ObjectSelectSlotView slot in _slotPool)
        {
            if (slot == null || slot.ObjectId < 0) continue;
            slot.SetPlacedCount(_database.GetPlacedCount(slot.ObjectId));
        }
    }
}
