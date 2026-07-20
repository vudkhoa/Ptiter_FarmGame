using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Map
{
    /// <summary>
    /// Khai báo mọi thứ module Map cần để chạy.
    /// Map không có service global — toàn bộ là component per-scene.
    /// </summary>
    public static class MapModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub (broker) do Map sở hữu.</summary>
        public static IContainerBuilder RegisterMapEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<MapPlacementStartedPayload>(options);
            builder.RegisterMessageBroker<MapPreviewMovedPayload>(options);
            builder.RegisterMessageBroker<MapFurnitureAddedPayload>(options);
            builder.RegisterMessageBroker<MapPlacementStoppedPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterMapModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMapEvents(options);
            return builder;
        }

        /// <summary>Component sống theo scene. Gọi ở GAME scope.</summary>
        public static IContainerBuilder RegisterMapSceneComponents(this IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MapService>()
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.RegisterComponentInHierarchy<MapPointerBridge>();
            builder.RegisterComponentInHierarchy<MapPreviewView>();

            return builder;
        }
    }
}
