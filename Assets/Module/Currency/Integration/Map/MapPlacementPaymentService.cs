using Core.Module.Map;
using Core.Module.Toast;

namespace Core.Module.Currency.Integration.Map
{
    public sealed class MapPlacementPaymentService : IMapPlacementPaymentService
    {
        private readonly ICurrencyService _currency;

        public int Balance => _currency.Balance;

        public MapPlacementPaymentService(ICurrencyService currency)
        {
            _currency = currency;
        }

        public bool TrySpend(int objectId, int amount)
        {
            if (_currency.Balance < amount)
            {
                ToastHub.Show(
                    $"Không đủ tiền! Cần {amount} vàng",
                    ToastStyle.Error);
                return false;
            }

            return _currency.TrySpend(amount, $"map-object:{objectId}");
        }

        public bool Refund(int objectId, int amount)
        {
            return _currency.AddCoins(amount, $"map-object-refund:{objectId}");
        }
    }
}
