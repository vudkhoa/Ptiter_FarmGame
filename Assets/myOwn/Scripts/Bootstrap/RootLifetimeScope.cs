using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Cross-scene root container. Sống xuyên scene (DontDestroyOnLoad).
    /// Register: MessagePipe + global Singleton services (PlayerDataHolder, ClockService).
    /// Đặt component này trên GameObject "[Bootstrap]" trong scene Preloading.
    /// </summary>
    /// <remarks>
    /// Lifecycle:
    /// - Awake: DontDestroyOnLoad TRƯỚC base.Awake() (để VContainer build container biết GO đã persist).
    /// - Configure: register tất cả singletons + RegisterMessagePipe() để pub/sub fail-loud.
    /// - GameLifetimeScope sẽ FindAnyObjectByType this rồi set parent → inherit registrations.
    /// </remarks>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

            builder.RegisterMessageBroker<PlayerDataLoadedPayload>(options);
            builder.RegisterMessageBroker<ClockTickPayload>(options);

            builder.Register<PlayerDataHolder>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<ClockService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}
