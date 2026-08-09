using System;
namespace Core.Module.Settings
{
    /// <summary>
    /// Owns the mock settings state for the current app session. It deliberately
    /// does not persist values or touch audio/haptic services yet.
    /// </summary>
    public sealed class SettingsService : ISettingsService
    {
        public const bool DefaultMusicEnabled = true;
        public const bool DefaultSoundEnabled = true;
        public const bool DefaultVibrationEnabled = true;

        private bool _musicEnabled = DefaultMusicEnabled;
        private bool _soundEnabled = DefaultSoundEnabled;
        private bool _vibrationEnabled = DefaultVibrationEnabled;

        public SettingsSnapshot Current => new SettingsSnapshot(
            _musicEnabled,
            _soundEnabled,
            _vibrationEnabled);

        public SettingsSnapshot SetEnabled(SettingsOption option, bool enabled)
        {
            switch (option)
            {
                case SettingsOption.Music:
                    _musicEnabled = enabled;
                    break;
                case SettingsOption.Sound:
                    _soundEnabled = enabled;
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
