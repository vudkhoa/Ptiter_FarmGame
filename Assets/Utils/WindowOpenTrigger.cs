using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using Shared.Utils.HUD;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public sealed class WindowOpenTrigger :
        MonoBehaviour,
        IHudButtonAction,
        IPointerDownHandler,
        IPointerClickHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private WindowsManager _windowsManager;
        [SerializeField] private UIWindow _window;
        [SerializeField] private bool _isOpen;

        private static readonly HashSet<WindowOpenTrigger> OpenTriggers =
            new HashSet<WindowOpenTrigger>();

        public static bool IsAnyWindowOpen => OpenTriggers.Count > 0;

        private IObjectResolver _resolver;
        private IInputService _inputService;
        private bool _windowEventsSubscribed;

        [Inject]
        public void Construct(
            IObjectResolver resolver,
            WindowsManager windowsManager,
            IInputService inputService)
        {
            _resolver = resolver;
            _windowsManager = windowsManager;
            _inputService = inputService;
            ResolveCanonicalWindowReference();
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
            ResolveCanonicalWindowReference();
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
            if (GetComponent<HudActionButton>() != null) return;

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
            // Legacy collider mouse callbacks are independent from uGUI's
            // raycast pipeline. Give UI controls priority when both overlap.
            if (_button != null ||
                PointerReleaseSuppression.IsPrimaryReleaseSuppressed ||
                IsPointerOverUI() ||
                IsAnotherWindowOpen())
                return;

            Open();
        }

        /// <summary>
        /// Reset suppression from the same EventSystem pointer stream that will
        /// later deliver the world click. Mobile no longer relies on legacy mouse
        /// emulation to begin a new primary-pointer gesture.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsWorldPrimaryPointer(eventData)) return;
            PointerReleaseSuppression.BeginPrimaryPress();
        }

        /// <summary>
        /// Handles mouse and touch through the scene EventSystem. World objects
        /// participate in that pipeline through the PhysicsRaycaster on the map
        /// camera, so mobile input no longer depends on touch-to-mouse emulation.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsWorldPrimaryPointer(eventData) ||
                PointerReleaseSuppression.IsPrimaryReleaseSuppressed ||
                IsAnotherWindowOpen())
                return;

            // Do not call IsPointerOverUI here. This callback is delivered only
            // after EventSystem has already selected this collider as the click
            // target; querying it now would report this world object itself.
            Open();
        }

        private bool IsWorldPrimaryPointer(PointerEventData eventData)
        {
            return _button == null &&
                   eventData != null &&
                   eventData.pointerPressRaycast.module is PhysicsRaycaster &&
                   eventData.button == PointerEventData.InputButton.Left;
        }

        public bool CanExecute()
        {
            return _window != null &&
                   _windowsManager != null &&
                   !IsAnotherWindowOpen();
        }

        public bool TryExecute()
        {
            if (_window == null)
            {
                Debug.LogError(
                    $"[WindowOpenTrigger] '{name}' has no UIWindow assigned.",
                    this);
                return false;
            }

            if (_windowsManager == null)
            {
                Debug.LogError(
                    $"[WindowOpenTrigger] '{name}' was not injected with a WindowsManager. " +
                    "Instantiate this prefab through VContainer.",
                    this);
                return false;
            }

            if (!ResolveCanonicalWindowReference())
            {
                Debug.LogError(
                    $"[WindowOpenTrigger] '{name}' could not resolve '{_window.name}' " +
                    "from the UI window collection.",
                    this);
                return false;
            }

            if (IsAnotherWindowOpen()) return false;

            SubscribeWindowEvents();
            if (!_windowsManager.Open(_window)) return false;

            SetOpenState(true);
            if (!_windowsManager.TryGetWindowInstance(
                    _window, out WindowController controller))
                return true;

            _resolver?.Inject(controller);
            return true;
        }

        public void Open() => TryExecute();

        private static bool HasOpenTrigger()
        {
            return IsAnyWindowOpen;
        }

        private bool IsPointerOverUI()
        {
            if (_inputService != null)
                return _inputService.IsPointerOverUI();

            if (_resolver == null ||
                !_resolver.TryResolve(out _inputService))
                return false;

            return _inputService.IsPointerOverUI();
        }

        private bool IsAnotherWindowOpen()
        {
            if (GameplayInputBlockRegistry.IsBlocked || HasOpenTrigger())
                return true;

            if (_inputService != null)
                return _inputService.IsGameplayInputBlocked;

            if (_resolver == null || !_resolver.TryResolve(out _inputService))
                return false;

            return _inputService.IsGameplayInputBlocked;
        }

        private void SubscribeWindowEvents()
        {
            if (_windowEventsSubscribed || _window == null ||
                _windowsManager == null)
                return;

            if (!ResolveCanonicalWindowReference()) return;

            // Addressable world prefabs can deserialize their own copy of a
            // ScriptableObjectCollection item in a player build. Subscribe through
            // the scene manager rather than UIWindow's non-serialized manager field.
            _windowsManager.SubscribeToWindowEvent(
                WindowEvent.WindowOpened, _window, OnWindowOpened);
            _windowsManager.SubscribeToWindowEvent(
                WindowEvent.WindowClosed, _window, OnWindowClosed);
            _windowEventsSubscribed = true;
        }

        private void UnsubscribeWindowEvents()
        {
            if (!_windowEventsSubscribed || _window == null ||
                _windowsManager == null)
                return;

            _windowsManager.UnsubscribeToWindowEvent(
                WindowEvent.WindowOpened, _window, OnWindowOpened);
            _windowsManager.UnsubscribeToWindowEvent(
                WindowEvent.WindowClosed, _window, OnWindowClosed);
            _windowEventsSubscribed = false;
        }

        private bool ResolveCanonicalWindowReference()
        {
            if (_window == null) return false;

            List<UIWindow> knownWindows = CollectionsRegistry.Instance
                .GetAllCollectionItemsOfType<UIWindow>();
            for (int i = 0; i < knownWindows.Count; i++)
            {
                UIWindow knownWindow = knownWindows[i];
                if (knownWindow == null || knownWindow.GUID != _window.GUID)
                    continue;

                _window = knownWindow;
                return true;
            }

            return false;
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
