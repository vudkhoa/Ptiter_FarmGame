using UnityEngine;
using Core.Module.Input;
using Core.Module.Map;
using Core.Module.Farm;
using Core.Module.Storage;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Core.Module.Time;
using Core.Module.Quest;
using myOwn.Firebase;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Cross-scene root container (DontDestroyOnLoad). Register MessagePipe + global Singleton services.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        [Header("Boot data (cấp cho preloader chạy lúc boot)")]
        [SerializeField] private ObjectDatabaseSO _objectDatabase;

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // MessagePipe infrastructure — enables IPublisher<T> / ISubscriber<T> injection.
            // Call ONCE here; installers only receive `options`, never call this themselves.
            var options = builder.RegisterMessagePipe();

            // Each module declares its own brokers + global services in {Module}ModuleInstaller.cs.
            builder.RegisterInputModule(options)
                   .RegisterMapModule(options)
                   .RegisterTimeModule(options)
                   .RegisterStorageModule(options)
                   .RegisterFarmModule(options)
                   .RegisterQuestModule(options);

            // ObjectCatalog (preloader) cần ObjectDatabaseSO qua constructor → phải có trong container.
            builder.RegisterInstance(_objectDatabase);

            #region App Block — không thuộc module nào (cùng assembly MyOwn với file này)
            builder.RegisterMessageBroker<PlayerDataLoadedPayload>(options);
            builder.RegisterMessageBroker<FirebaseReadyPayload>(options);

            // AsImplementedInterfaces() → mọi interface (IService, IAsyncStartable, ITickable...) visible cho consumer + entry-point dispatcher.
            // AsSelf() → cho phép inject qua concrete type.
            builder.Register<PlayerDataHolder>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            // FirebaseInitService: IAsyncStartable → tự chạy CheckAndFixDependencies lúc container build.
            // AsImplementedInterfaces để lộ IAsyncStartable (tự StartAsync) + IFirebaseGate (consumer inject).
            builder.Register<FirebaseInitService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<FirebaseCloudService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            #endregion
        }
    }
}
