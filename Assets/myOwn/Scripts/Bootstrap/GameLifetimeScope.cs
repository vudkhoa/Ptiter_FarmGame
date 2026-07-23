using Core.Module.Map;
using Core.Module.Farm;
using Core.Module.Quest;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Per-scene container that parents itself to RootLifetimeScope to inherit its registrations.
    /// Built manually by MapSceneBootstrap, so autoRun must stay off in the Inspector.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            if (parentReference.Object == null)
            {
                parentReference.Object = FindAnyObjectByType<RootLifetimeScope>();

                if (parentReference.Object == null)
                    Debug.LogWarning("[GameLifetimeScope] No RootLifetimeScope found - global services will not resolve. Start the game from the Preloading scene.");
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Scene-scoped components only; global brokers are inherited from RootLifetimeScope.
            // FarmDatabaseSO + QuestCatalogSO are enqueued by MapSceneBootstrap right before Build() is called.
            builder.RegisterMapSceneComponents()
                   .RegisterFarmGameplay()
                   .RegisterQuestGameplay();
        }
    }
}
