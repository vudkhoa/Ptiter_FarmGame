using System.Collections.Generic;
using BrunoMikoski.UIManager;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Shared.Utils
{
    /// <summary>
    /// Reusable trigger that opens a UIManager window from either a UI Button or
    /// a world object with a Collider (via OnMouseUpAsButton).
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
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
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
        private void Start() => SubscribeWindowEvents();

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
            if (_windowsManager == null || _window == null) return;

            SubscribeWindowEvents();
            if (!_windowsManager.Open(_window)) return;

            SetOpenState(true);
            if (!_windowsManager.TryGetWindowInstance(
                    _window, out WindowController controller))
                return;

            ResolveContainer()?.Inject(controller);
        }

        private static bool HasOpenTrigger()
        {
            return OpenTriggers.Count > 0;
        }

        private void SubscribeWindowEvents()
        {
            if (_windowEventsSubscribed || _window == null) return;

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

        private IObjectResolver ResolveContainer()
        {
            if (_resolver != null) return _resolver;

            LifetimeScope[] scopes = FindObjectsByType<LifetimeScope>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[i] != null && scopes[i].Container != null &&
                    scopes[i].name == "GameLifetimeScope")
                {
                    _resolver = scopes[i].Container;
                    return _resolver;
                }
            }

            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[i] != null && scopes[i].Container != null)
                {
                    _resolver = scopes[i].Container;
                    return _resolver;
                }
            }

            return null;
        }
    }
}
