using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Time
{
    /// <summary>
    /// Khai báo mọi thứ module Time cần để chạy (clock tick, đồng bộ giờ server, phát hiện chỉnh giờ).
    /// </summary>
    public static class TimeModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub (broker) do Time sở hữu.</summary>
        public static IContainerBuilder RegisterTimeEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<ClockTickPayload>(options);
            builder.RegisterMessageBroker<ServerTimeSyncedPayload>(options);
            builder.RegisterMessageBroker<ClockManipulationDetectedPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterTimeModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterTimeEvents(options);

            builder.Register<WebTimeSyncSource>(Lifetime.Singleton)
                   .AsImplementedInterfaces();

            // ClockService / ServerTimeService là MonoBehaviour → config gắn trên GameObject,
            // GameObject phải nằm dưới hierarchy của [Bootstrap].
            builder.RegisterComponentInHierarchy<ClockService>()
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.RegisterComponentInHierarchy<ServerTimeService>()
                   .AsImplementedInterfaces()
                   .AsSelf();

            return builder;
        }
    }
}
