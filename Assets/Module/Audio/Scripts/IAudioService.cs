using UnityEngine;

namespace Core.Module.Audio
{
    public interface IAudioService
    {
        bool IsMusicPlaying { get; }

        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlayMusic(
            AudioClip clip,
            float volume = 1f,
            bool restartIfSame = false,
            bool loop = true);
        void StopMusic();
        void StopAll();
    }
}
