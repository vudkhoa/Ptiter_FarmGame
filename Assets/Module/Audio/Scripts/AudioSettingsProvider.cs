using System;
using UnityEngine;

namespace Core.Module.Audio
{
    public sealed class AudioSettingsProvider : IAudioSettingsProvider, IDisposable
    {
        private const string KeyPrefix = "audio.";
        private static readonly AudioBus[] Buses =
        {
            AudioBus.Master, AudioBus.Music, AudioBus.Sfx
        };

        private readonly AudioSettingsSO _defaults;
        private readonly float[] _volumes = new float[Buses.Length];
        private readonly bool[] _muted = new bool[Buses.Length];

        public event Action<AudioBus> Changed;

        public AudioSettingsProvider(AudioSettingsSO defaults)
        {
            _defaults = defaults;
            Load();
        }

        public float GetVolume(AudioBus bus) => _volumes[ToIndex(bus)];

        public void SetVolume(AudioBus bus, float volume)
        {
            int index = ToIndex(bus);
            float next = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_volumes[index], next)) return;

            _volumes[index] = next;
            PlayerPrefs.SetFloat(VolumeKey(bus), next);
            Changed?.Invoke(bus);
        }

        public bool IsMuted(AudioBus bus) => _muted[ToIndex(bus)];

        public void SetMuted(AudioBus bus, bool muted)
        {
            int index = ToIndex(bus);
            if (_muted[index] == muted) return;

            _muted[index] = muted;
            PlayerPrefs.SetInt(MuteKey(bus), muted ? 1 : 0);
            Changed?.Invoke(bus);
        }

        public bool ToggleMuted(AudioBus bus)
        {
            bool muted = !IsMuted(bus);
            SetMuted(bus, muted);
            return muted;
        }

        public void ResetToDefaults()
        {
            foreach (AudioBus bus in Buses)
            {
                int index = ToIndex(bus);
                _volumes[index] = DefaultVolume(bus);
                _muted[index] = false;
                PlayerPrefs.SetFloat(VolumeKey(bus), _volumes[index]);
                PlayerPrefs.SetInt(MuteKey(bus), 0);
                Changed?.Invoke(bus);
            }
            PlayerPrefs.Save();
        }

        public void Dispose() => PlayerPrefs.Save();

        private void Load()
        {
            foreach (AudioBus bus in Buses)
            {
                int index = ToIndex(bus);
                _volumes[index] = Mathf.Clamp01(
                    PlayerPrefs.GetFloat(VolumeKey(bus), DefaultVolume(bus)));
                _muted[index] = PlayerPrefs.GetInt(MuteKey(bus), 0) != 0;
            }
        }

        private float DefaultVolume(AudioBus bus) =>
            _defaults != null ? _defaults.GetDefaultVolume(bus) : 1f;

        private static int ToIndex(AudioBus bus)
        {
            int index = (int)bus;
            if (index < 0 || index >= Buses.Length)
                throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            return index;
        }

        private static string VolumeKey(AudioBus bus) =>
            $"{KeyPrefix}{bus.ToString().ToLowerInvariant()}.volume";

        private static string MuteKey(AudioBus bus) =>
            $"{KeyPrefix}{bus.ToString().ToLowerInvariant()}.muted";
    }
}
