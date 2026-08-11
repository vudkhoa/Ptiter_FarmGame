using VContainer;

namespace Core.Module.Settings
{
    public static class SettingsModuleInstaller
    {
        /// <summary>Registers settings backed by the already-registered audio module.</summary>
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
