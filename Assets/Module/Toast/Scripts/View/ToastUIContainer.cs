using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Toast
{
    /// The persistent home of the toast layer, authored into the Preloading scene so a message
    /// raised during a scene swap still lands. Owns the pool, the stack order and the overflow queue.
    [DisallowMultipleComponent]
    public sealed class ToastUIContainer : MonoBehaviour, IToastView
    {
        [Header("Wiring (assigned by Tools/Toast/Rebuild Toast Content)")]
        [SerializeField] private ToastConfigSO _config;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _stack;
        [SerializeField] private ToastItemView _itemPrefab;

        [Header("Layout")]
        [Tooltip("Canvas units from the middle of the screen. Positive lifts the stack.")]
        [SerializeField] private float _centerOffsetY = 0f;
        [SerializeField] private float _spacing = 16f;

        [Tooltip("Off when it already sits under a persistent root.")]
        [SerializeField] private bool _persistAcrossScenes = true;

        private readonly List<ToastItemView> _active = new();
        private readonly List<ToastItemView> _pool = new();
        private readonly Queue<ToastRequest> _queued = new();

        #region Properties
        public bool IsShowing => _active.Count > 0;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_stack == null) _stack = transform as RectTransform;

            // Excluded from GlobalModalInputBlocker's max scan; without this every modal that
            // opens would be lifted above the toast layer and swallow the message it raised.
            ToastCanvasRegistry.Register(_canvas);

            if (_itemPrefab == null || _config == null)
            {
                Debug.LogError(
                    "[ToastUIContainer] Missing prefab or config - toasts will not show. " +
                    "Run Tools/Toast/Rebuild Toast Content.", this);
            }

            // Guard the double-persist case: an object already under a DontDestroyOnLoad root is
            // moved out of that root by a second DontDestroyOnLoad call.
            if (_persistAcrossScenes && transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() => ToastCanvasRegistry.Unregister(_canvas);
        #endregion

        #region Public API
        public void Show(in ToastRequest request)
        {
            if (_config == null || _itemPrefab == null || !request.IsValid) return;

            if (_config.mergeDuplicates && TryRestartDuplicate(request)) return;

            if (_active.Count >= _config.maxVisible)
            {
                Enqueue(request);
                return;
            }

            Spawn(request);
        }

        public void HideAll()
        {
            _queued.Clear();

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ToastItemView item = _active[i];
                if (item == null) continue;

                item.HideImmediate();
                _pool.Add(item);
            }

            _active.Clear();
        }
        #endregion

        #region Event Handlers
        private void OnItemFinished(ToastItemView item)
        {
            if (item == null) return;

            _active.Remove(item);
            _pool.Add(item);
            Reflow();

            if (_queued.Count == 0) return;

            Spawn(_queued.Dequeue());
        }
        #endregion

        #region Private Methods
        private void Spawn(in ToastRequest request)
        {
            ToastItemView item = Rent();
            if (item == null) return;

            _active.Add(item);

            // Played at the slot it will occupy, then the whole stack re-flows: the bubbles above
            // have to move up by exactly this one's height, only known after it lays out.
            item.Play(request, _centerOffsetY);
            Reflow();
        }

        private bool TryRestartDuplicate(in ToastRequest request)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                ToastItemView item = _active[i];
                if (item == null || item.Message != request.Message) continue;

                item.Restart(request.Duration);
                return true;
            }

            return false;
        }

        private void Enqueue(in ToastRequest request)
        {
            if (_config.maxQueued <= 0) return;

            // Drops the oldest waiting message rather than the newest: a burst of notifications
            // is worth less than whatever just happened under the player's finger.
            while (_queued.Count >= _config.maxQueued) _queued.Dequeue();

            _queued.Enqueue(request);
        }

        /// Newest sits on the centre line; every older bubble is pushed up above it.
        private void Reflow()
        {
            float y = _centerOffsetY;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ToastItemView item = _active[i];
                if (item == null) continue;

                item.MoveTo(y);
                y += item.Height + _spacing;
            }
        }

        private ToastItemView Rent()
        {
            int last = _pool.Count - 1;
            while (last >= 0)
            {
                ToastItemView pooled = _pool[last];
                _pool.RemoveAt(last);
                if (pooled != null) return pooled;
                last = _pool.Count - 1;
            }

            ToastItemView item = Instantiate(_itemPrefab, _stack);
            item.Configure(_config, OnItemFinished);
            return item;
        }
        #endregion
    }
}
