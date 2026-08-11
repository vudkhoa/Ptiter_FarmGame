using BrunoMikoski.UIManager;
using Core.Module.Input;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Settings.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsWindowController :
        WindowController,
        IOnBeforeWindowOpen,
        IOnWindowClosed
    {
        [Header("Navigation")]
        [SerializeField] private Button _closeButton;

        [Header("Runtime-only toggles")]
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private RectTransform _musicToggleVisual;
        [SerializeField] private RectTransform _soundToggleVisual;
        [SerializeField] private RectTransform _vibrationToggleVisual;
        [SerializeField] private Image _musicToggleImage;
        [SerializeField] private Image _soundToggleImage;
        [SerializeField] private Image _vibrationToggleImage;
        [SerializeField] private Color _enabledColor = Color.white;
        [SerializeField] private Color _disabledColor =
            new Color(0.55f, 0.48f, 0.43f, 1f);

        private ISettingsService _settingsService;
        private bool _listenersRegistered;
        private bool _constructed;

        [Inject]
        public void Construct(ISettingsService settingsService)
        {
            if (_constructed) return;
            _constructed = true;
            _settingsService = settingsService;
            BindSnapshot(_settingsService.Current);
        }

        public void OnBeforeWindowOpen()
        {
            GameplayInputBlockRegistry.Add(this);
            RegisterListeners();
            if (_settingsService != null)
                BindSnapshot(_settingsService.Current);
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterListeners();
        }

        private void RegisterListeners()
        {
            if (_listenersRegistered) return;
            _listenersRegistered = true;
            _closeButton?.onClick.AddListener(Close);
            _musicToggle?.onValueChanged.AddListener(OnMusicChanged);
            _soundToggle?.onValueChanged.AddListener(OnSoundChanged);
            _vibrationToggle?.onValueChanged.AddListener(OnVibrationChanged);
        }

        private void UnregisterListeners()
        {
            if (!_listenersRegistered) return;
            _listenersRegistered = false;
            _closeButton?.onClick.RemoveListener(Close);
            _musicToggle?.onValueChanged.RemoveListener(OnMusicChanged);
            _soundToggle?.onValueChanged.RemoveListener(OnSoundChanged);
            _vibrationToggle?.onValueChanged.RemoveListener(OnVibrationChanged);
        }

        private void OnMusicChanged(bool enabled)
        {
            ApplyChange(SettingsOption.Music, enabled);
        }

        private void OnSoundChanged(bool enabled)
        {
            ApplyChange(SettingsOption.Sound, enabled);
        }

        private void OnVibrationChanged(bool enabled)
        {
            ApplyChange(SettingsOption.Vibration, enabled);
        }

        private void ApplyChange(SettingsOption option, bool enabled)
        {
            if (_settingsService != null)
                BindSnapshot(_settingsService.SetEnabled(option, enabled));
            else
                ApplyToggleVisual(option, enabled);
        }

        private void BindSnapshot(SettingsSnapshot snapshot)
        {
            _musicToggle?.SetIsOnWithoutNotify(snapshot.MusicEnabled);
            _soundToggle?.SetIsOnWithoutNotify(snapshot.SoundEnabled);
            _vibrationToggle?.SetIsOnWithoutNotify(snapshot.VibrationEnabled);
            ApplyToggleVisual(SettingsOption.Music, snapshot.MusicEnabled);
            ApplyToggleVisual(SettingsOption.Sound, snapshot.SoundEnabled);
            ApplyToggleVisual(
                SettingsOption.Vibration, snapshot.VibrationEnabled);
        }

        private void ApplyToggleVisual(SettingsOption option, bool enabled)
        {
            RectTransform visual;
            Image image;
            switch (option)
            {
                case SettingsOption.Music:
                    visual = _musicToggleVisual;
                    image = _musicToggleImage;
                    break;
                case SettingsOption.Sound:
                    visual = _soundToggleVisual;
                    image = _soundToggleImage;
                    break;
                case SettingsOption.Vibration:
                    visual = _vibrationToggleVisual;
                    image = _vibrationToggleImage;
                    break;
                default:
                    return;
            }

            if (visual != null)
            {
                Vector3 scale = visual.localScale;
                scale.x = enabled ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                visual.localScale = scale;
            }

            if (image != null)
                image.color = enabled ? _enabledColor : _disabledColor;
        }

        protected override void OnDestroy()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterListeners();
            base.OnDestroy();
        }
    }
}
