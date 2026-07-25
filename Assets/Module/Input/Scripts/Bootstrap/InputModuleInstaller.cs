using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Input
{
    /// <summary>
    /// Khai báo mọi thứ module Input cần để chạy.
    /// Quy ước: 1 payload chỉ được đăng ký tại đúng module publish ra nó.
    /// </summary>
    public static class InputModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub (broker) do Input sở hữu.</summary>
        public static IContainerBuilder RegisterInputEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<PointerScreenPayload>(options);
            builder.RegisterMessageBroker<PointerButtonDownPayload>(options);
            builder.RegisterMessageBroker<KeyDownPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterInputModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterInputEvents(options);

            // InputService là MonoBehaviour → GameObject phải nằm dưới hierarchy của [Bootstrap].
            builder.RegisterComponentInHierarchy<InputService>()
                   .AsImplementedInterfaces()
                   .AsSelf();

            return builder;
        }
    }
}
