using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Currency
{
    public static class CurrencyModuleInstaller
    {
        public static IContainerBuilder RegisterCurrencyEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<CurrencyTransactionRequestedPayload>(options);
            builder.RegisterMessageBroker<CurrencyBalanceSetRequestedPayload>(options);
            builder.RegisterMessageBroker<CurrencyTransactionProcessedPayload>(options);
            builder.RegisterMessageBroker<CurrencyChangedPayload>(options);
            return builder;
        }

        public static IContainerBuilder RegisterCurrencyModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterCurrencyEvents(options);
            builder.RegisterEntryPoint<CurrencyService>()
                   .AsSelf();
            builder.RegisterEntryPoint<CurrencyHudController>()
                   .AsSelf();
            return builder;
        }
    }
}
