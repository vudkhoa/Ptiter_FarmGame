using System;

namespace Core.Module.Audio
{
    public interface IAudioSettingsProvider
    {
        event Action<AudioBus> Changed;

        float GetVolume(AudioBus bus);
        void SetVolume(AudioBus bus, float volume);
        bool IsMuted(AudioBus bus);
        void SetMuted(AudioBus bus, bool muted);
        bool ToggleMuted(AudioBus bus);
        void ResetToDefaults();
    }
}
