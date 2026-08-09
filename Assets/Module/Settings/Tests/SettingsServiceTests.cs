using NUnit.Framework;

namespace Core.Module.Settings.Tests
{
    public sealed class SettingsServiceTests
    {
        [Test]
        public void NewService_EnablesAllOptionsByDefault()
        {
            var service = new SettingsService();

            Assert.That(service.Current.MusicEnabled, Is.True);
            Assert.That(service.Current.SoundEnabled, Is.True);
            Assert.That(service.Current.VibrationEnabled, Is.True);
        }

        [TestCase(SettingsOption.Music)]
        [TestCase(SettingsOption.Sound)]
        [TestCase(SettingsOption.Vibration)]
        public void SetEnabled_UpdatesRequestedOption(SettingsOption option)
        {
            var service = new SettingsService();

            SettingsSnapshot result = service.SetEnabled(option, false);

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
            var service = new SettingsService();

            SettingsSnapshot result = service.SetEnabled(
                SettingsOption.Music, false);

            Assert.That(result.SoundEnabled, Is.True);
            Assert.That(result.VibrationEnabled, Is.True);
        }
    }
}
