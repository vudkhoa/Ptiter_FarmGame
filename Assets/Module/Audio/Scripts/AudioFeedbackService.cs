using System;
using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Audio
{
    public sealed class AudioFeedbackService : IInitializable, IDisposable
    {
        private readonly IAudioService _audio;
        private readonly AudioCatalogSO _catalog;
        private AudioClip _errorFallback;

        public AudioFeedbackService(IAudioService audio, AudioCatalogSO catalog)
        {
            _audio = audio;
            _catalog = catalog;
        }

        public void Initialize()
        {
            AudioUiFeedback.Bind(this);
        }

        public void PlayClick(float volume = 1f) =>
            _audio.PlaySfx(_catalog.ButtonClick, volume);

        public void PlayError()
        {
            _errorFallback ??= CreateErrorFallback();
            _audio.PlaySfx(_catalog.Error != null ? _catalog.Error : _errorFallback);
        }

        public void Dispose()
        {
            AudioUiFeedback.Unbind(this);
            if (_errorFallback != null)
                UnityEngine.Object.Destroy(_errorFallback);
        }

        private static AudioClip CreateErrorFallback()
        {
            const int sampleRate = 22050;
            const float duration = 0.16f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float progress = i / (float)sampleCount;
                float frequency = Mathf.Lerp(360f, 180f, progress);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) *
                             (1f - progress) * 0.22f;
            }

            AudioClip clip = AudioClip.Create(
                "Generated Error SFX", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    internal static class AudioUiFeedback
    {
        private static AudioFeedbackService _service;

        public static void Bind(AudioFeedbackService service) => _service = service;

        public static void Unbind(AudioFeedbackService service)
        {
            if (ReferenceEquals(_service, service))
                _service = null;
        }

        public static void PlayClick(float volume) => _service?.PlayClick(volume);

        public static void PlayError() => _service?.PlayError();
    }
}
