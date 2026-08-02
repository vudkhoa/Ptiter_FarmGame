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
        /// Service + view theo scene. Gọi ở GAME scope.
        /// Không cần enqueue gì: catalog lấy qua ICutsceneCatalogProvider kế thừa từ ROOT scope.
        /// </summary>
        /// <param name="view">Kéo ở GameLifetimeScope. Để trống = scene chưa dựng UI cutscene, bỏ qua cả module.</param>
        /// <param name="skipInput">Optional - không có nút skip thì để trống.</param>
        /// <param name="autoPlayer">Optional - chỉ cần khi muốn scene tự phát cutscene lúc mở.</param>
        public static IContainerBuilder RegisterCutsceneGameplay(
            this IContainerBuilder builder,
            CutsceneView view,
            CutsceneSkipInput skipInput = null,
            CutsceneAutoPlayer autoPlayer = null)
        {
            if (view == null)
            {
                Debug.LogWarning("[CutsceneModuleInstaller] GameLifetimeScope chưa gán CutsceneView - bỏ qua module Cutscene.");
                return builder;
            }

            builder.Register<CutsceneRunner>(Lifetime.Singleton)
                   .AsSelf();
            builder.Register<CutsceneService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            // RegisterComponent tự kèm build-callback ép inject, y như RegisterComponentInHierarchy.
            builder.RegisterComponent(view)
                   .AsImplementedInterfaces()
                   .AsSelf();

            if (skipInput != null) builder.RegisterComponent(skipInput);
            if (autoPlayer != null) builder.RegisterComponent(autoPlayer);

            builder.RegisterEntryPoint<CutsceneRequestListener>();

            return builder;
        }
    }
}
