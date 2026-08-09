using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapPlacementCancelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IMapService _map;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(
            IMapService map,
            ISubscriber<MapPlacementStartedPayload> startSub,
            ISubscriber<MapPlacementStoppedPayload> stopSub,
            ISubscriber<MapPlayerRemovalModeChangedPayload> removalModeSub)
        {
            _map = map;
            _button.onClick.AddListener(CancelPlacement);

            var bag = DisposableBag.CreateBuilder();
            startSub.Subscribe(_ => RefreshVisibility()).AddTo(bag);
            stopSub.Subscribe(_ => RefreshVisibility()).AddTo(bag);
            removalModeSub.Subscribe(_ => RefreshVisibility()).AddTo(bag);
            _subscriptions = bag.Build();

            RefreshVisibility();
        }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(CancelPlacement);
            _subscriptions?.Dispose();
        }

        private void CancelPlacement()
        {
            _map?.StopPlacement();
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void RefreshVisibility()
        {
            SetVisible(_map != null && _map.HasActivePlacement);
        }
    }
}
