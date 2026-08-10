using NUnit.Framework;
using Core.Module.Audio;
using System;

namespace Core.Module.Settings.Tests
{
    public sealed class SettingsServiceTests
    {
        private FakeAudioSettingsProvider _audioSettings;
        private SettingsService _service;

        [SetUp]
        public void SetUp()
        {
            _audioSettings = new FakeAudioSettingsProvider();
            _service = new SettingsService(_audioSettings);
        }

        [Test]
        public void NewService_EnablesAllOptionsByDefault()
        {
            Assert.That(_service.Current.MusicEnabled, Is.True);
            Assert.That(_service.Current.SoundEnabled, Is.True);
            Assert.That(_service.Current.VibrationEnabled, Is.True);
        }

        [TestCase(SettingsOption.Music)]
        [TestCase(SettingsOption.Sound)]
        [TestCase(SettingsOption.Vibration)]
        public void SetEnabled_UpdatesRequestedOption(SettingsOption option)
        {
            SettingsSnapshot result = _service.SetEnabled(option, false);

            bool actual = option switch
            {
                SettingsOption.Music => result.MusicEnabled,
                SettingsOption.Sound => result.SoundEnabled,
                SettingsOption.Vibration => result.VibrationEnabled,
                _ => true
            };
            Assert.That(actual, Is.False);
        }

        [Test]
        public void SetEnabled_DoesNotChangeOtherOptions()
        {
            SettingsSnapshot result = _service.SetEnabled(
                SettingsOption.Music, false);

            Assert.That(result.SoundEnabled, Is.True);
            Assert.That(result.VibrationEnabled, Is.True);
        }

        [Test]
        public void Current_ReflectsAudioStateChangedOutsideSettingsService()
        {
            _audioSettings.SetMuted(AudioBus.Music, true);
            _audioSettings.SetMuted(AudioBus.Sfx, true);

            Assert.That(_service.Current.MusicEnabled, Is.False);
            Assert.That(_service.Current.SoundEnabled, Is.False);
        }

        private sealed class FakeAudioSettingsProvider : IAudioSettingsProvider
        {
            private readonly float[] _volumes = { 1f, 1f, 1f };
            private readonly bool[] _muted = new bool[3];

            public event Action<AudioBus> Changed;

            public float GetVolume(AudioBus bus) => _volumes[(int)bus];

            public void SetVolume(AudioBus bus, float volume)
            {
                _volumes[(int)bus] = volume;
                Changed?.Invoke(bus);
            }

            public bool IsMuted(AudioBus bus) => _muted[(int)bus];

            public void SetMuted(AudioBus bus, bool muted)
            {
                _muted[(int)bus] = muted;
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
                for (int i = 0; i < _volumes.Length; i++)
                {
                    _volumes[i] = 1f;
                    _muted[i] = false;
                    Changed?.Invoke((AudioBus)i);
                }
            }
        }
    }
}
