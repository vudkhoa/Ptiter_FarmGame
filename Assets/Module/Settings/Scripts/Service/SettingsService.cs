using System;
using Core.Module.Audio;

namespace Core.Module.Settings
{
    /// <summary>
    /// Exposes player-facing settings. Audio state is owned and persisted by the
    /// audio module; vibration remains runtime-only until a haptics service exists.
    /// </summary>
    public sealed class SettingsService : ISettingsService
    {
        public const bool DefaultVibrationEnabled = true;

        private readonly IAudioSettingsProvider _audioSettings;
        private bool _vibrationEnabled = DefaultVibrationEnabled;

        public SettingsService(IAudioSettingsProvider audioSettings)
        {
            _audioSettings = audioSettings ??
                throw new ArgumentNullException(nameof(audioSettings));
        }

        public SettingsSnapshot Current => new SettingsSnapshot(
            !_audioSettings.IsMuted(AudioBus.Music),
            !_audioSettings.IsMuted(AudioBus.Sfx),
            _vibrationEnabled);

        public SettingsSnapshot SetEnabled(SettingsOption option, bool enabled)
        {
            switch (option)
            {
                case SettingsOption.Music:
                    _audioSettings.SetMuted(AudioBus.Music, !enabled);
                    break;
                case SettingsOption.Sound:
                    _audioSettings.SetMuted(AudioBus.Sfx, !enabled);
                    break;
                case SettingsOption.Vibration:
                    _vibrationEnabled = enabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, null);
            }

            return Current;
        }
    }
}
