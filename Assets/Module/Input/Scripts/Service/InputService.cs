using UnityEngine;
using UnityEngine.EventSystems;
using MessagePipe;
using System.Collections.Generic;
using VContainer;
using UInput = UnityEngine.Input;

namespace Core.Module.Input
{
    [DisallowMultipleComponent]
    public sealed class InputService : MonoBehaviour, IInputService
    {
        [SerializeField] private InputConfigSO _config;

        private IPublisher<PointerScreenPayload> _pubScreen;
        private IPublisher<PointerButtonDownPayload> _pubButton;
        private IPublisher<KeyDownPayload> _pubKey;

        private Vector2 _lastPointerScreen;
        private readonly bool[] _heldButtons = new bool[3];
        private Dictionary<KeyCode, bool> _heldKeys;

        #region DI - Construct
        [Inject]
        public void Construct(
            IPublisher<PointerScreenPayload> pubScreen,
            IPublisher<PointerButtonDownPayload> pubButton,
            IPublisher<KeyDownPayload> pubKey)
        {
            _pubScreen = pubScreen;
            _pubButton = pubButton;
            _pubKey = pubKey;
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[InputService] _config chưa được drag vào Inspector — service inert.");
                enabled = false;
                return;
            }

            _heldKeys = new Dictionary<KeyCode, bool>(_config.WatchedKeys.Count);

            if (_config.WatchedKeys.Count == 0)
            {
                Debug.LogWarning("[InputService] WatchedKeys empty — không có key nào được publish.");
            }
            else
            {
                for (int i = 0; i < _config.WatchedKeys.Count; i++)
                    _heldKeys[_config.WatchedKeys[i]] = false;
            }
        }

        private void Update()
        {
            PollPointerScreen();
            PollButtons();
            PollWatchedKeys();
        }
        #endregion

        #region IInputService - Query API
        public Vector2 PointerScreen => _lastPointerScreen;

        public bool IsButtonHeld(int button)
        {
            if (button < 0 || button >= _heldButtons.Length) return false;
            return _heldButtons[button];
        }

        public bool IsKeyHeld(KeyCode key)
        {
            return _heldKeys != null
                && _heldKeys.TryGetValue(key, out var v) && v;
        }

        public bool IsPointerOverUI()
        {
            return EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
        }
        #endregion

        #region Polling
        private void PollPointerScreen()
        {
            Vector2 cur = UInput.mousePosition;
            Vector2 delta = cur - _lastPointerScreen;
            float sqrEpsilon = _config.PointerMoveEpsilon * _config.PointerMoveEpsilon;

            if (delta.sqrMagnitude > sqrEpsilon)
            {
                _lastPointerScreen = cur;
                _pubScreen.Publish(new PointerScreenPayload(_lastPointerScreen));
            }
        }

        private void PollButtons()
        {
            for (int i = 0; i < _heldButtons.Length; i++)
            {
                if (UInput.GetMouseButtonDown(i))
                    _pubButton.Publish(new PointerButtonDownPayload(i));

                _heldButtons[i] = UInput.GetMouseButton(i);
            }
        }

        private void PollWatchedKeys()
        {
            var keys = _config.WatchedKeys;
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (UInput.GetKeyDown(key))
                    _pubKey.Publish(new KeyDownPayload(key));

                _heldKeys[key] = UInput.GetKey(key);
            }
        }
        #endregion
    }
}
