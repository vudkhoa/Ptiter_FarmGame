using MessagePipe;
using System;
using UnityEngine;
using VContainer;

namespace Core.Module.Map
{
    [DisallowMultipleComponent]
    public sealed class MapPreviewView : MonoBehaviour
    {

        [SerializeField] private Material _previewMaterial;
        [SerializeField] private CursorIndicatorBehavior _cursor;
        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private float _previewYOffset = 0.06f;

        private GameObject _ghost;
        private Material _previewMatInstance;
        private MaterialPropertyBlock _block;
        private IDisposable _subscriptions;

        #region DI - Constructor
        [Inject]
        public void Construct(
            ISubscriber<MapPlacementStartedPayload> startSub,
            ISubscriber<MapPreviewMovedPayload> moveSub,
            ISubscriber<MapFurnitureAddedPayload> addedSub,
            ISubscriber<MapPlacementStoppedPayload> stopSub)
        {
            var bag = DisposableBag.CreateBuilder();
            startSub.Subscribe(OnStarted).AddTo(bag);
            moveSub.Subscribe(OnMoved).AddTo(bag);
            addedSub.Subscribe(OnAdded).AddTo(bag);
            stopSub.Subscribe(OnStopped).AddTo(bag);
            _subscriptions = bag.Build();

        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_previewMaterial != null) _previewMatInstance = new Material(_previewMaterial);
            if (_cursor != null) _cursor.GameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
            if (_previewMatInstance != null) Destroy(_previewMatInstance);
        }
        #endregion

        #region Preview View - Logic
        private void OnStarted(MapPlacementStartedPayload p)
        {
            _ghost = Instantiate(p.Prefab);
            SwapToPreviewMaterial(_ghost);
            _cursor.transform.localScale = new Vector3(p.Size.x, 1, p.Size.y);
            _cursor.Renderer.GetPropertyBlock(_block);
            _block.SetVector("_MainTex_ST", new Vector4(p.Size.x, p.Size.y, 0, 0));
            _cursor.Renderer.SetPropertyBlock(_block);
            _cursor.GameObject.SetActive(true);
        }

        private void OnMoved(MapPreviewMovedPayload p)
        {
            if (_ghost == null) return;
            _ghost.transform.position = new Vector3(p.SnappedWorld.x, p.SnappedWorld.y + _previewYOffset, p.SnappedWorld.z);
            _cursor.transform.position = p.SnappedWorld;
            _cursor.Renderer.GetPropertyBlock(_block);
            _block.SetColor("_Color", p.IsValid ? Color.white : Color.red);
            _cursor.Renderer.SetPropertyBlock(_block);
        }

        private void OnAdded(MapFurnitureAddedPayload p)
        {
            var go = Instantiate(p.Prefab, _spawnRoot);
            go.transform.position = p.SnappedWorld;
        }

        private void OnStopped(MapPlacementStoppedPayload _)
        {
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
            if (_cursor != null) _cursor.GameObject.SetActive(false);
        }

        private void SwapToPreviewMaterial(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                var mats = rends[i].materials;
                for (int j = 0; j < mats.Length; j++) mats[j] = _previewMatInstance;
                rends[i].materials = mats;
            }
        }
        #endregion
    }
}