using UnityEngine;

namespace Core.Module.Audio
{
    public interface IAudioService
    {
        AudioSource Play(AudioCue cue);
        void PlayMusic(AudioCue cue, bool restartIfSame = false);
        void StopMusic();
        void StopAll();
    }
}
