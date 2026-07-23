using MessagePipe;
using VContainer;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Core.Module.Quest.Utils;
using VContainer.Unity;
#endif

namespace Core.Module.Quest
{
    /// <summary>
    /// Wires up the Quest module. Root chỉ giữ broker dùng chung;
    /// QuestService và rules sống ở gameplay scene, catalog do MapSceneBootstrap enqueue vào Game scope.
    /// </summary>
    public static class QuestModuleInstaller
    {
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

        /// <summary>Global brokers. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterQuestModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterQuestEvents(options);
            return builder;
        }

        /// <summary>
        /// Service + rules theo scene. Gọi ở GAME scope.
        /// Yêu cầu QuestCatalogSO được enqueue vào cùng scope trước khi Build().
        /// </summary>
        public static IContainerBuilder RegisterQuestGameplay(this IContainerBuilder builder)
        {
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
    }
}
