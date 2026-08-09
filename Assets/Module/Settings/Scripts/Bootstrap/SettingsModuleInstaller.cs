using VContainer;

namespace Core.Module.Settings
{
    public static class SettingsModuleInstaller
    {
        /// <summary>Registers the cross-scene, runtime-only settings state.</summary>
        public static IContainerBuilder RegisterSettingsModule(
            this IContainerBuilder builder)
        {
            builder.Register<SettingsService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            return builder;
        }
    }
}
