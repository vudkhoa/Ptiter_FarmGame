using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Audio
{
    public static class AudioModuleInstaller
    {
        public static IContainerBuilder RegisterAudioModule(
            this IContainerBuilder builder,
            AudioSettingsSO settings = null)
        {
            settings ??= ScriptableObject.CreateInstance<AudioSettingsSO>();
            builder.RegisterInstance(settings);
            builder.Register<AudioSettingsProvider>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
            builder.RegisterEntryPoint<AudioService>()
                   .AsSelf();
            return builder;
        }
    }
}
