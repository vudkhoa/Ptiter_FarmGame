using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public sealed class FarmBgmController : IInitializable
    {
        private readonly IAudioService _audio;
        private readonly AudioCatalogSO _catalog;

        public FarmBgmController(IAudioService audio, AudioCatalogSO catalog)
        {
            _audio = audio;
            _catalog = catalog;
        }

        public void Initialize() => _audio.PlayMusic(_catalog.FarmMusic);
    }
}
