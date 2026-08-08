using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapPlayerRemoveButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IMapService _map;
        private MapAuthoringController _authoring;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(
            IMapService map,
            MapAuthoringController authoring,
            ISubscriber<MapPlacementStartedPayload> startSub,
            ISubscriber<MapPlacementStoppedPayload> stopSub,
            ISubscriber<MapPlayerRemovalModeChangedPayload> removalModeSub)
        {
            _map = map;
            _authoring = authoring;
            _button.onClick.AddListener(BeginRemoval);

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
            if (_button != null) _button.onClick.RemoveListener(BeginRemoval);
            _subscriptions?.Dispose();
        }

        private void BeginRemoval()
        {
            _map?.SetPlayerRemovalMode(true);
        }

        private void RefreshVisibility()
        {
            bool visible = _map != null
                && (_authoring == null || !_authoring.IsAuthoringMode)
                && !_map.HasActivePlacement
                && !_map.IsPlayerRemovalMode;
            gameObject.SetActive(visible);
        }
    }
}
