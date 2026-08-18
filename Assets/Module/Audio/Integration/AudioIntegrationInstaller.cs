using VContainer;
using VContainer.Unity;

namespace Core.Module.Audio.Integration
{
    public static class AudioIntegrationInstaller
    {
        public static IContainerBuilder RegisterAudioIntegration(
            this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<FarmBgmController>();
            builder.RegisterEntryPoint<FarmAudioBridge>();
            builder.RegisterEntryPoint<MapAudioBridge>();
            builder.RegisterEntryPoint<EconomyAudioBridge>();
            return builder;
        }
    }
}
