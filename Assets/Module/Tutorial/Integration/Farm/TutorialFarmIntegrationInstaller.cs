using VContainer;
using VContainer.Unity;

namespace Core.Module.Tutorial.Integration.Farm
{
    /// <summary>
    /// Lives in the integration assembly so the Tutorial module never has to reference Farm or Map.
    /// Call at GAME scope: the bridge needs the per-scene IMapService and IFarmService.
    /// </summary>
    public static class TutorialFarmIntegrationInstaller
    {
        public static IContainerBuilder RegisterTutorialFarmIntegration(this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<TutorialGameplaySignalBridge>();
            return builder;
        }
    }
}
