using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Farm
{
    /// <summary>
    /// Khai báo mọi thứ module Farm cần để chạy.
    /// Root chỉ giữ broker (kênh dùng chung). FarmDatabaseSO do MapSceneBootstrap nạp bằng Addressables
    /// rồi Enqueue vào Game scope — FarmService và view sống theo scene gameplay.
    /// </summary>
    public static class FarmModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub (broker) do Farm sở hữu.</summary>
        public static IContainerBuilder RegisterFarmEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<FarmSlotChangedPayload>(options);
            builder.RegisterMessageBroker<OpenFarmSelectorUIPayload>(options);
            return builder;
        }

        /// <summary>Broker global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterFarmModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterFarmEvents(options);
            return builder;
        }

        /// <summary>
        /// Service + component sống theo scene. Gọi ở GAME scope.
        /// Điều kiện: FarmDatabaseSO phải được Enqueue vào scope này trước khi Build.
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
