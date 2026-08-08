using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Audio
{
    public static class AudioModuleInstaller
    {
        public static IContainerBuilder RegisterAudioModule(
            this IContainerBuilder builder,
            AudioSettingsSO settings = null,
            AudioCatalogSO catalog = null)
        {
            settings ??= ScriptableObject.CreateInstance<AudioSettingsSO>();
            catalog ??= ScriptableObject.CreateInstance<AudioCatalogSO>();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(catalog);
            builder.Register<AudioSettingsProvider>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
            builder.RegisterEntryPoint<AudioService>()
                   .AsSelf();
            return builder;
        }
    }
}
