using UnityEngine;
using MyOwn.ServiceHarness;
using VContainer.Unity;
using MessagePipe;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UInput = UnityEngine.Input;

namespace Core.Module.Input
{
    public sealed class InputService : IService, IInputService, ITickable
    {
        private readonly IPublisher<PointerScreenPayload> _pubScreen;
        private readonly IPublisher<PointerButtonDownPayload> _pubButton;
        private readonly IPublisher<KeyDownPayload> _pubKey;
        private readonly InputConfigSO _config;

        private Vector2 _lastPointerScreen;
        private readonly bool[] _heldButtons = new bool[3];
        private readonly Dictionary<KeyCode, bool> _heldKeys;

        #region Core.Input - Constructor
        public InputService(IPublisher<PointerScreenPayload> pubScreen,
                            IPublisher<PointerButtonDownPayload> pubButton,
                            IPublisher<KeyDownPayload> pubKey,
                            InputConfigSO config)
        {
            _pubScreen = pubScreen;
            _pubButton = pubButton;
            _pubKey = pubKey;
            _config = config;

            _heldKeys = new Dictionary<KeyCode, bool>(config.WatchedKeys.Count);

            if (config.WatchedKeys.Count == 0)
            {
                Debug.LogWarning("[InputService] WatchedKeys empty");
            }
            else
            {
                for (int i = 0; i < config.WatchedKeys.Count; i++)
                    _heldKeys[config.WatchedKeys[i]] = false;
            }

        }
        #endregion

        #region Core.Input - Logic/Implement Interface
        public Vector2 PointerScreen => _lastPointerScreen;

        public bool IsButtonHeld(int button)
        {
            if (button < 0 || button >= _heldButtons.Length) return false;
            return _heldButtons[button];
        }

        public bool IsKeyHeld(KeyCode key)
        {
            return _heldKeys.TryGetValue(key, out var result) && result;
        }

        public bool IsPointerOverUI()
        {
            return EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject();
        }

        //(*)(*)(*) Important
        public void Tick()
        {
            PollPointerScreen();
            PollButton();
            PollWatchedKeys();
        }

        private void PollPointerScreen()
        {
            Vector2 cur = UInput.mousePosition;

            // Epsilon Logic
            Vector2 delta = cur - _lastPointerScreen;
            float sqrEpsilon = _config.PointerMoveEpsilon * _config.PointerMoveEpsilon;
            if (delta.sqrMagnitude > sqrEpsilon)
            {
                _lastPointerScreen = cur;
                _pubScreen.Publish(new PointerScreenPayload(_lastPointerScreen));
            }
        }

        private void PollButton()
        {
            for (int i = 0; i < _heldButtons.Length; ++i)
            {
                if (UInput.GetMouseButtonDown(i))
                {
                    _pubButton.Publish(new PointerButtonDownPayload(i));
                }

                _heldButtons[i] = UInput.GetMouseButton(i);
            }
        }

        private void PollWatchedKeys()
        {
            var keys = _config.WatchedKeys;
            for (int i = 0; i < _config.WatchedKeys.Count; ++i)
            {
                var key = keys[i];
                if (UInput.GetKeyDown(key))
                {
                    _pubKey.Publish(new KeyDownPayload(key));
                }
                _heldKeys[key] = UInput.GetKey(key);
            }
        }
        #endregion
    }
}