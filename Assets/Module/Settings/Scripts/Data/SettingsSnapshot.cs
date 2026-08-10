namespace Core.Module.Settings
{
    /// <summary>
    /// Current player-facing toggle state. Music and sound are backed by the
    /// audio module; vibration is runtime-only until haptics are implemented.
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
