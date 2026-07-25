using MessagePipe;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Core.Module.Quest.Utils;
using VContainer.Unity;
#endif

namespace Core.Module.Quest
{
    /// <summary>
    /// Khai báo mọi thứ module Quest cần để chạy.
    /// Toàn bộ là service global (Root) — Quest không có component per-scene.
    /// </summary>
    public static class QuestModuleInstaller
    {
        private const string QuestCatalogResourcePath = "QuestCatalog";

        /// <summary>Chỉ mở các kênh pub/sub (broker) do Quest sở hữu.</summary>
        public static IContainerBuilder RegisterQuestEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<QuestAcceptedPayload>(options);
            builder.RegisterMessageBroker<QuestProgressChangedPayload>(options);
            builder.RegisterMessageBroker<QuestCompletedPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterQuestModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterQuestEvents(options);

            builder.RegisterInstance(LoadQuestCatalog());

            builder.Register<QuestProgressApplier>(Lifetime.Singleton)
                   .AsSelf();
            builder.Register<StateReachedObjectiveRule>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            builder.Register<QuestObjectiveRuleRegistry>(Lifetime.Singleton)
                   .AsSelf();
            builder.Register<QuestCompletionEvaluator>(Lifetime.Singleton)
                   .AsSelf();
            builder.Register<QuestService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterEntryPoint<FarmQuestTestFlow>();
            builder.RegisterEntryPoint<QuestTestPanelBootstrap>();
#endif

            return builder;
        }

        private static QuestCatalogSO LoadQuestCatalog()
        {
            var catalog = Resources.Load<QuestCatalogSO>(QuestCatalogResourcePath);
            if (catalog == null)
            {
                Debug.LogWarning("[RootLifetimeScope] QuestCatalog SO not found in Resources. Creating a temporary runtime instance to prevent container crash. Make sure to place one at 'Assets/Resources/QuestCatalog.asset' or another Resources folder.");
                catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            }
            return catalog;
        }
    }
}
