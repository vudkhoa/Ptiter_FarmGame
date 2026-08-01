using VContainer;

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
            return builder;
        }
    }
}
