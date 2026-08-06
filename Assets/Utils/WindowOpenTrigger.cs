using System.Collections.Generic;
using BrunoMikoski.UIManager;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Shared.Utils
{
    /// <summary>
    /// Reusable trigger that opens a UIManager window from either a UI Button or
    /// a world object with a Collider (via OnMouseUpAsButton). The UIWindow is
    /// serialized in the prefab; the scene-owned WindowsManager is injected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WindowOpenTrigger : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private WindowsManager _windowsManager;
        [SerializeField] private UIWindow _window;
        [SerializeField] private bool _isOpen;

        private static readonly HashSet<WindowOpenTrigger> OpenTriggers =
            new HashSet<WindowOpenTrigger>();
        private IObjectResolver _resolver;
        private bool _windowEventsSubscribed;

        [Inject]
        public void Construct(
            IObjectResolver resolver,
            WindowsManager windowsManager)
        {
            _resolver = resolver;
            _windowsManager = windowsManager;
        }

        public void Configure(
            WindowsManager windowsManager,
            UIWindow window,
            Button button = null,
            IObjectResolver resolver = null)
        {
            UnregisterButton();
            UnsubscribeWindowEvents();
            _windowsManager = windowsManager;
            _window = window;
            _button = button;
            _resolver = resolver ?? _resolver;
            RegisterButton();
            SubscribeWindowEvents();
        }

        private void OnEnable() => RegisterButton();

        private void Start()
        {
            SubscribeWindowEvents();
        }

        private void OnDisable()
        {
            UnregisterButton();
            UnsubscribeWindowEvents();
            SetOpenState(false);
        }

        private void RegisterButton()
        {
            _button?.onClick.RemoveListener(Open);
            _button?.onClick.AddListener(Open);
        }

        private void UnregisterButton()
        {
            _button?.onClick.RemoveListener(Open);
        }

        // Unity invokes this only after a press/release on the same Collider.
        private void OnMouseUpAsButton()
        {
            if (_button != null || HasOpenTrigger())
                return;

            Open();
        }

        public void Open()
        {
            if (_window == null)
            {
                Debug.LogError(
                    $"[WindowOpenTrigger] '{name}' has no UIWindow assigned.",
                    this);
                return;
            }

            if (_windowsManager == null)
            {
                Debug.LogError(
                    $"[WindowOpenTrigger] '{name}' was not injected with a WindowsManager. " +
                    "Instantiate this prefab through VContainer.",
                    this);
                return;
            }

            SubscribeWindowEvents();
            if (!_windowsManager.Open(_window)) return;

            SetOpenState(true);
            if (!_windowsManager.TryGetWindowInstance(
                    _window, out WindowController controller))
                return;

            _resolver?.Inject(controller);
        }

        private static bool HasOpenTrigger()
        {
            return OpenTriggers.Count > 0;
        }

        private void SubscribeWindowEvents()
        {
            if (_windowEventsSubscribed || _window == null ||
                _windowsManager == null)
                return;

            _window.OnOpenedEvent += OnWindowOpened;
            _window.OnClosedEvent += OnWindowClosed;
            _windowEventsSubscribed = true;
        }

        private void UnsubscribeWindowEvents()
        {
            if (!_windowEventsSubscribed || _window == null) return;

            _window.OnOpenedEvent -= OnWindowOpened;
            _window.OnClosedEvent -= OnWindowClosed;
            _windowEventsSubscribed = false;
        }

        private void OnWindowOpened() => SetOpenState(true);
        private void OnWindowClosed() => SetOpenState(false);

        private void SetOpenState(bool value)
        {
            _isOpen = value;
            if (value)
                OpenTriggers.Add(this);
            else
                OpenTriggers.Remove(this);
        }

    }
}
