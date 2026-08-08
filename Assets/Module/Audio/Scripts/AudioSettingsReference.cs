using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Audio
{
    /// <summary>Binds one settings row to a Slider and an optional mute Toggle.</summary>
    public sealed class AudioSettingsReference : MonoBehaviour
    {
        [SerializeField] private AudioBus _bus = AudioBus.Master;
        [SerializeField] private Slider _volumeSlider;
        [Tooltip("Toggle ON means this bus is muted.")]
        [SerializeField] private Toggle _muteToggle;

        private IAudioSettingsProvider _settings;

        [Inject]
        public void Construct(IAudioSettingsProvider settings)
        {
            Unbind();
            _settings = settings;
            Bind();
            Refresh();
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable() => Unbind();

        public void SetVolume(float value) => _settings?.SetVolume(_bus, value);
        public void SetMuted(bool muted) => _settings?.SetMuted(_bus, muted);
        public void ToggleMuted() => _settings?.ToggleMuted(_bus);
        public void ResetAll() => _settings?.ResetToDefaults();

        private void Bind()
        {
            if (_settings == null) return;
            _settings.Changed -= OnSettingsChanged;
            _settings.Changed += OnSettingsChanged;

            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0f;
                _volumeSlider.maxValue = 1f;
                _volumeSlider.onValueChanged.RemoveListener(SetVolume);
                _volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.RemoveListener(SetMuted);
                _muteToggle.onValueChanged.AddListener(SetMuted);
            }
        }

        private void Unbind()
        {
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;
            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.RemoveListener(SetVolume);
            if (_muteToggle != null)
                _muteToggle.onValueChanged.RemoveListener(SetMuted);
        }

        private void OnSettingsChanged(AudioBus bus)
        {
            if (bus == _bus || bus == AudioBus.Master) Refresh();
        }

        private void Refresh()
        {
            if (_settings == null) return;
            _volumeSlider?.SetValueWithoutNotify(_settings.GetVolume(_bus));
            _muteToggle?.SetIsOnWithoutNotify(_settings.IsMuted(_bus));
        }
    }
}
