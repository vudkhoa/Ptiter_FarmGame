using VContainer;
using VContainer.Unity;

namespace Core.Module.Currency.Integration.Quest
{
    public static class CurrencyQuestIntegrationInstaller
    {
        public static IContainerBuilder RegisterCurrencyQuestIntegration(
            this IContainerBuilder builder)
        {
            builder.Register<QuestCurrencyEventBridge>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();
            builder.RegisterEntryPoint<QuestProgressCurrencyBridge>()
                   .AsSelf();
            return builder;
        }
    }
}
