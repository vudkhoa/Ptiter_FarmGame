using MessagePipe;
using VContainer;

namespace Core.Module.Storage
{
    /// <summary>
    /// Khai báo mọi thứ module Storage cần để chạy.
    /// Hiện Storage chỉ định nghĩa payload — service lưu trữ nằm ở tầng app (PlayerDataHolder).
    /// </summary>
    public static class StorageModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub (broker) do Storage sở hữu.</summary>
        public static IContainerBuilder RegisterStorageEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<InventoryChangedPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterStorageModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterStorageEvents(options);
            return builder;
        }
    }
}
