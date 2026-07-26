using MessagePipe;
using VContainer;

namespace Core.Module.Loading
{
    public static class LoadingModuleInstaller
    {
        /// <summary>
        /// Gọi ở ROOT scope. LoadingConfigSO được RegisterInstance ở RootLifetimeScope.
        /// LoadingService lộ ILoadingService + IAssetLoader + IDisposable.
        /// </summary>
        public static IContainerBuilder RegisterLoadingModule(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<LoadingProgressPayload>(options);
            builder.Register<LoadingService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            return builder;
        }
    }
}
