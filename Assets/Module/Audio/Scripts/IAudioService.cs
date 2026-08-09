using UnityEngine;

namespace Core.Module.Audio
{
    public interface IAudioService
    {
        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlayMusic(
            AudioClip clip,
            float volume = 1f,
            bool restartIfSame = false);
        void StopMusic();
        void StopAll();
    }
}
