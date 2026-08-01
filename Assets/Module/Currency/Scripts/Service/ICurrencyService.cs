namespace Core.Module.Currency
{
    public interface ICurrencyService
    {
        int Balance { get; }

        bool AddCoins(int amount, string reason = null);
        bool TrySpend(int amount, string reason = null);
    }
}
