using Core.Module.Input;
using Core.Module.Map;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Core.Module.Time;
using myOwn.Firebase;

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

            // Map brokers
            builder.RegisterMessageBroker<MapPlacementStartedPayload>(options);
            builder.RegisterMessageBroker<MapPreviewMovedPayload>(options);
            builder.RegisterMessageBroker<MapFurnitureAddedPayload>(options);
            builder.RegisterMessageBroker<MapPlacementStoppedPayload>(options);

            // Time
            builder.RegisterMessageBroker<ServerTimeSyncedPayload>(options);

            // Firebase
            builder.RegisterMessageBroker<FirebaseReadyPayload>(options);

            // AsImplementedInterfaces() → mọi interface (IService, IAsyncStartable, ITickable, IInputService...) visible cho consumer + entry-point dispatcher.
            // AsSelf() → cho phép inject qua concrete type.
            builder.Register<PlayerDataHolder>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            #region Time Block
            builder.Register<LocalTimeSyncSource>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            builder.RegisterComponentInHierarchy<ClockService>()
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterComponentInHierarchy<ServerTimeService>()
                .AsImplementedInterfaces()
                .AsSelf();
            #endregion

            #region Firebase Block
            // FirebaseInitService: IAsyncStartable → tự chạy CheckAndFixDependencies lúc container build.
            // AsImplementedInterfaces để lộ IAsyncStartable (tự StartAsync) + IFirebaseGate (consumer inject).
            builder.Register<FirebaseInitService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<FirebaseCloudService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            #endregion

            // MonoBehaviour services — config tự gắn trên GameObject của service (xem README §6).
            // GameObject phải nằm trong hierarchy của LifetimeScope này (child của [Bootstrap]).
            builder.RegisterComponentInHierarchy<InputService>()
                .AsImplementedInterfaces()
                .AsSelf();
        }
    }
}
