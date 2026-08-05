using BrunoMikoski.UIManager;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Root giữ broker + catalog provider; service và view sống ở gameplay scene.
    /// </summary>
    public static class CutsceneModuleInstaller
    {
        /// <summary>Chỉ mở các kênh pub/sub do Cutscene sở hữu.</summary>
        public static IContainerBuilder RegisterCutsceneEvents(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<PlayCutsceneRequestPayload>(options);
            builder.RegisterMessageBroker<CutsceneStartedPayload>(options);
            builder.RegisterMessageBroker<CutsceneStepChangedPayload>(options);
            builder.RegisterMessageBroker<CutsceneFinishedPayload>(options);
            return builder;
        }

        /// <summary>Global brokers + preloader. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterCutsceneModule(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterCutsceneEvents(options);

            builder.Register<CutsceneCatalogProvider>(Lifetime.Singleton)
                   .AsImplementedInterfaces()   // ICutsceneCatalogProvider + IBootPreloader
                   .AsSelf();

            return builder;
        }

        /// <summary>
        /// Service + view provider theo scene. Gọi ở GAME scope.
        /// Không cần enqueue gì: catalog lấy qua ICutsceneCatalogProvider kế thừa từ ROOT scope.
        /// View KHÔNG nằm sẵn trong scene - provider tự Instantiate ở lần Play đầu tiên,
        /// window reference lấy từ CutsceneCatalogSO nên installer không cần tham số nào.
        /// Trigger (CutscenePlayButton, CutsceneAutoPlayer) KHÔNG đăng ký ở đây: không ai resolve chúng,
        /// chúng chỉ cần được inject -> thả vào Auto Inject Game Objects của LifetimeScope là đủ.
        /// </summary>
        public static IContainerBuilder RegisterCutsceneGameplay(this IContainerBuilder builder)
        {
            builder.Register<CutsceneRunner>(Lifetime.Singleton)
                   .AsSelf();
            builder.Register<CutsceneService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.Register<CutsceneViewProvider>(Lifetime.Singleton)
                   .AsImplementedInterfaces();

            builder.RegisterEntryPoint<CutsceneRequestListener>();

            return builder;
        }
    }
}
