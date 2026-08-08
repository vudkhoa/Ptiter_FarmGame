using VContainer;
using VContainer.Unity;

namespace Core.Module.Storage.Integration.Farm
{
    public static class FarmStorageIntegrationInstaller
    {
        public static IContainerBuilder RegisterFarmStorageIntegration(
            this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<FarmHarvestStorageBridge>();
            return builder;
        }
    }
}
