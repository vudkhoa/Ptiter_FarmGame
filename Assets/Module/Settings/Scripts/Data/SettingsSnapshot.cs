namespace Core.Module.Settings
{
    /// <summary>
    /// Runtime-only toggle state. Applying audio and vibration to platform
    /// services is intentionally outside the current UI mock scope.
    /// </summary>
    public readonly struct SettingsSnapshot
    {
        public bool MusicEnabled { get; }
        public bool SoundEnabled { get; }
        public bool VibrationEnabled { get; }

        public SettingsSnapshot(
            bool musicEnabled,
            bool soundEnabled,
            bool vibrationEnabled)
        {
            MusicEnabled = musicEnabled;
            SoundEnabled = soundEnabled;
            VibrationEnabled = vibrationEnabled;
        }
    }
}
