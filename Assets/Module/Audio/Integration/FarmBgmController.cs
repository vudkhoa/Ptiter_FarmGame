using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public sealed class FarmBgmController : IInitializable, ITickable
    {
        private readonly IAudioService _audio;
        private readonly AudioCatalogSO _catalog;
        private AudioClip _currentBgm;

        public FarmBgmController(IAudioService audio, AudioCatalogSO catalog)
        {
            _audio = audio;
            _catalog = catalog;
        }

        public void Initialize() => PlayNextBgm();

        public void Tick()
        {
            if (_currentBgm != null && !_audio.IsMusicPlaying)
                PlayNextBgm();
        }

        private void PlayNextBgm()
        {
            AudioClip nextBgm = _catalog.GetRandomBgm(_currentBgm);
            if (nextBgm == null)
            {
                _currentBgm = null;
                return;
            }

            _currentBgm = nextBgm;
            _audio.PlayMusic(_currentBgm, loop: false);
        }
    }
}
