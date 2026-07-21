using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Farm
{
    /// <summary>
    /// Wires up the Farm module. Root holds only the shared brokers;
    /// FarmService and its views live in the gameplay scene, with the database enqueued by MapSceneBootstrap.
    /// </summary>
    public static class FarmModuleInstaller
    {
        /// <summary>Opens the pub/sub channels owned by Farm.</summary>
        public static IContainerBuilder RegisterFarmEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<FarmSlotChangedPayload>(options);
            builder.RegisterMessageBroker<OpenFarmSelectorUIPayload>(options);
            return builder;
        }

        /// <summary>Global brokers. Call this from the ROOT scope.</summary>
        public static IContainerBuilder RegisterFarmModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterFarmEvents(options);
            return builder;
        }

        /// <summary>
        /// Scene-scoped service and components. Call this from the GAME scope.
        /// Requires FarmDatabaseSO to be enqueued into the same scope before Build().
        /// </summary>
        public static IContainerBuilder RegisterFarmGameplay(this IContainerBuilder builder)
        {
            builder.Register<FarmService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.RegisterComponentInHierarchy<FarmInputHandler>();
            builder.RegisterComponentInHierarchy<FarmVisualizer>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterComponentInHierarchy<FarmDebugLogger>();
            builder.RegisterComponentInHierarchy<FarmTestHelper>();
#endif

            return builder;
        }
    }
}
