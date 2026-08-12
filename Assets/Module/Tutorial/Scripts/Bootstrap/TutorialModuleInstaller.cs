using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Service and view live at ROOT because the tutorial spans scene loads; the gameplay
    /// bridge lives in the GAME scope because it needs the per-scene IMapService.
    /// </summary>
    public static class TutorialModuleInstaller
    {
        /// <summary>Only the channels the tutorial owns. Farm/Map brokers belong to their own modules.</summary>
        public static IContainerBuilder RegisterTutorialEvents(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<TutorialStepStartedPayload>(options);
            builder.RegisterMessageBroker<TutorialStepCompletedPayload>(options);
            builder.RegisterMessageBroker<TutorialFlowCompletedPayload>(options);
            return builder;
        }

        /// <summary>
        /// Global service + persistent view. Call at ROOT scope.
        /// A missing catalog or container costs the tutorial, never the boot: both degrade to
        /// no-ops so a half-set-up project still reaches gameplay.
        /// </summary>
        public static IContainerBuilder RegisterTutorialModule(
            this IContainerBuilder builder,
            MessagePipeOptions options,
            TutorialCatalogSO catalog,
            TutorialUIContainer container)
        {
            builder.RegisterTutorialEvents(options);

            if (catalog == null)
            {
                Debug.LogWarning(
                    "[TutorialModuleInstaller] No TutorialCatalogSO on RootLifetimeScope - tutorial disabled. " +
                    "Run Tools/Tutorial/Rebuild Tutorial Content.");

                // RegisterInstance reads the runtime type off the instance, so a null would throw
                // during Configure. An empty disabled catalog keeps the graph resolvable.
                catalog = ScriptableObject.CreateInstance<TutorialCatalogSO>();
                catalog.name = "TutorialCatalog (Missing)";
                catalog.tutorialEnabled = false;
            }
            builder.RegisterInstance(catalog);

            if (container != null)
            {
                builder.RegisterInstance(container).AsImplementedInterfaces().AsSelf();
            }
            else
            {
                Debug.LogWarning(
                    "[TutorialModuleInstaller] No TutorialUIContainer on RootLifetimeScope - " +
                    "the tutorial will run headless. Run Tools/Tutorial/Rebuild Tutorial Content.");
                builder.Register<NullTutorialView>(Lifetime.Singleton).AsImplementedInterfaces();
            }

            // RegisterEntryPoint already binds every interface, ITutorialService included;
            // adding AsImplementedInterfaces here would register each of them twice.
            builder.RegisterEntryPoint<TutorialService>()
                   .AsSelf();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterEntryPoint<TutorialDebugController>();
#endif

            return builder;
        }
    }
}
