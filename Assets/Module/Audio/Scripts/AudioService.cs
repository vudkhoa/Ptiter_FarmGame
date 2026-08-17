using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Audio
{
    /// <summary>2D audio: one music source and a small pool for SFX/UI.</summary>
    public sealed class AudioService : IAudioService, IInitializable, IDisposable
    {
        private readonly IAudioSettingsProvider _settings;
        private readonly AudioSettingsSO _config;
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();

        private GameObject _host;
        private AudioSource _musicSource;
        private float _musicCueVolume = 1f;
        private int _nextSfxIndex;

        public bool IsMusicPlaying =>
            _musicSource != null && _musicSource.isPlaying;

        public AudioService(
            IAudioSettingsProvider settings,
            AudioSettingsSO config)
        {
            _settings = settings;
            _config = config;
        }

        public void Initialize()
        {
            if (_host != null) return;

            _host = new GameObject("[AudioService]");
            UnityEngine.Object.DontDestroyOnLoad(_host);

            _musicSource = CreateSource("Music");
            _musicSource.loop = true;

            int poolSize = _config != null ? _config.SfxPoolSize : 8;
            for (int i = 0; i < poolSize; i++)
                _sfxPool.Add(CreateSource($"SFX {i + 1}"));

            _settings.Changed += OnSettingsChanged;
            RefreshVolumes();
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            EnsureInitialized();
            if (clip == null) return;

            AudioSource source = GetAvailableSfxSource();
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayMusic(
            AudioClip clip,
            float volume = 1f,
            bool restartIfSame = false,
            bool loop = true)
        {
            EnsureInitialized();
            if (clip == null) return;
            if (!restartIfSame && _musicSource.isPlaying && _musicSource.clip == clip)
                return;

            _musicCueVolume = Mathf.Clamp01(volume);
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            RefreshVolumes();
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource == null) return;
            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public void StopAll()
        {
            _musicSource?.Stop();
            for (int i = 0; i < _sfxPool.Count; i++)
                _sfxPool[i].Stop();
        }

        public void Dispose()
        {
            _settings.Changed -= OnSettingsChanged;
            if (_host != null)
                UnityEngine.Object.Destroy(_host);
            _host = null;
            _sfxPool.Clear();
        }

        private AudioSource GetAvailableSfxSource()
        {
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                int index = (_nextSfxIndex + i) % _sfxPool.Count;
                if (!_sfxPool[index].isPlaying)
                {
                    _nextSfxIndex = (index + 1) % _sfxPool.Count;
                    return _sfxPool[index];
                }
            }

            AudioSource source = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Count;
            source.Stop();
            return source;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(_host.transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnSettingsChanged(AudioBus _) => RefreshVolumes();

        private void RefreshVolumes()
        {
            if (_musicSource == null) return;

            float master = GetBusVolume(AudioBus.Master);
            _musicSource.volume = master * GetBusVolume(AudioBus.Music) *
                                  _musicCueVolume;

            float sfxVolume = master * GetBusVolume(AudioBus.Sfx);
            for (int i = 0; i < _sfxPool.Count; i++)
                _sfxPool[i].volume = sfxVolume;
        }

        private float GetBusVolume(AudioBus bus)
        {
            return _settings.IsMuted(bus) ? 0f : _settings.GetVolume(bus);
        }

        private void EnsureInitialized()
        {
            if (_host == null) Initialize();
        }
    }
}
