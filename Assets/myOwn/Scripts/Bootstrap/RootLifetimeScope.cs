using UnityEngine;
using Core.Module.Cutscene;
using Core.Module.Input;
using Core.Module.Map;
using Core.Module.Farm;
using Core.Module.Storage;
using Core.Module.Storage.Integration.Farm;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Core.Module.Time;
using Core.Module.Quest;
using Core.Module.Loading;
using Core.Module.Currency;
using Core.Module.Currency.Integration.Map;
using Core.Module.Currency.Integration.Quest;
using Core.Module.Settings;
using Core.Module.Audio;
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
        [SerializeField] private FarmDatabaseReference _farmDatabaseRef;
        [SerializeField] private QuestCatalogReference _questCatalogRef;
        [SerializeField] private CutsceneCatalogReference _cutsceneCatalogRef;
        [SerializeField] private AudioSettingsSO _audioSettings;
        [SerializeField] private AudioCatalogSO _audioCatalog;

        protected override void Awake()
        {
            // Build while still in this scene so RegisterComponentInHierarchy can find scene objects
            // (PreloadingFlow, LoadingScreenView). Then persist the root across scene loads.
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // MessagePipe infrastructure — enables IPublisher<T> / ISubscriber<T> injection.
            // Call ONCE here; installers only receive `options`, never call this themselves.
            var options = builder.RegisterMessagePipe();

            // Each module declares its own brokers + global services in {Module}ModuleInstaller.cs.
            builder.RegisterInputModule(options)
                   .RegisterAudioModule(_audioSettings, _audioCatalog)
                   .RegisterMapModule(options)
                   .RegisterTimeModule(options)
                   .RegisterStorageModule(options)
                   .RegisterSettingsModule()
                   .RegisterCurrencyModule(options)
                   .RegisterFarmModule(options)
                   .RegisterFarmStorageIntegration()
                   .RegisterQuestModule(options)
                   .RegisterCutsceneModule(options)
                   .RegisterCurrencyQuestIntegration()
                   .RegisterCurrencyMapIntegration()
                   .RegisterLoadingModule(options);

            // Boot data + Addressable refs cấp cho preloader (nạp trong RunBootSequenceAsync).
            builder.RegisterInstance(_objectDatabase);
            builder.RegisterInstance(_farmDatabaseRef);
            builder.RegisterInstance(_questCatalogRef);
            builder.RegisterInstance(_cutsceneCatalogRef);

            // Objects sống trong scene Preloading → đăng ký để được inject.
            builder.RegisterComponentInHierarchy<PreloadingFlow>();
            builder.RegisterComponentInHierarchy<LoadingScreenView>();

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
