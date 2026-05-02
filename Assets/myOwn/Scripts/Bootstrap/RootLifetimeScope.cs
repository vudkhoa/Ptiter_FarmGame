using Core.Module.Input;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Cross-scene root container (DontDestroyOnLoad). Register MessagePipe + global Singleton services.
    /// Đặt trên GameObject "[Bootstrap]" tại root level của scene Preloading.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            // DontDestroyOnLoad TRƯỚC base.Awake() để container persist xuyên scene.
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Cho phép inject IPublisher<T> / ISubscriber<T>.
            var options = builder.RegisterMessagePipe();

            // 1 broker = 1 kênh pub/sub. Quên → inject IPublisher<T> throw lúc resolve.
            builder.RegisterMessageBroker<PlayerDataLoadedPayload>(options);
            builder.RegisterMessageBroker<ClockTickPayload>(options);

            // Input module brokers
            builder.RegisterMessageBroker<PointerScreenPayload>(options);
            builder.RegisterMessageBroker<PointerButtonDownPayload>(options);
            builder.RegisterMessageBroker<KeyDownPayload>(options);

            // AsImplementedInterfaces() → IAsyncStartable visible → auto-call StartAsync().
            // AsSelf() → cho phép inject qua concrete type.
            builder.Register<PlayerDataHolder>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<ClockService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            // MonoBehaviour services — config tự gắn trên GameObject của service (xem README §6).
            // GameObject phải nằm trong hierarchy của LifetimeScope này (child của [Bootstrap]).
            builder.RegisterComponentInHierarchy<InputService>()
                .AsImplementedInterfaces()
                .AsSelf();
        }

        private void RegisService()
        {

        }
    }
}
