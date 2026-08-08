namespace Core.Module.Settings
{
    public interface ISettingsService
    {
        SettingsSnapshot Current { get; }

        SettingsSnapshot SetEnabled(SettingsOption option, bool enabled);
    }
}
