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
            ISubscriber<MapPlacementStoppedPayload> stopSub)
        {
            _map = map;
            _button.onClick.AddListener(CancelPlacement);

            var bag = DisposableBag.CreateBuilder();
            startSub.Subscribe(_ => SetVisible(true)).AddTo(bag);
            stopSub.Subscribe(_ => SetVisible(false)).AddTo(bag);
            _subscriptions = bag.Build();

            SetVisible(_map.HasActivePlacement);
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
    }
}
