using VContainer;

namespace Core.Module.Currency.Integration.Map
{
    public static class CurrencyMapIntegrationInstaller
    {
        public static IContainerBuilder RegisterCurrencyMapIntegration(
            this IContainerBuilder builder)
        {
            builder.Register<MapPlacementPaymentService>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
            return builder;
        }
    }
}
