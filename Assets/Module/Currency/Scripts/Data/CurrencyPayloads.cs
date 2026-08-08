namespace Core.Module.Currency
{
    public readonly struct CurrencyTransactionRequestedPayload
    {
        public readonly string TransactionId;
        public readonly CurrencyTransactionType Type;
        public readonly int Amount;
        public readonly string Reason;

        public CurrencyTransactionRequestedPayload(
            string transactionId,
            CurrencyTransactionType type,
            int amount,
            string reason)
        {
            TransactionId = transactionId;
            Type = type;
            Amount = amount;
            Reason = reason;
        }
    }

    public readonly struct CurrencyBalanceSetRequestedPayload
    {
        public readonly string TransactionId;
        public readonly int Balance;
        public readonly string Reason;

        public CurrencyBalanceSetRequestedPayload(
            string transactionId,
            int balance,
            string reason)
        {
            TransactionId = transactionId;
            Balance = balance;
            Reason = reason;
        }
    }

    public readonly struct CurrencyTransactionProcessedPayload
    {
        public readonly string TransactionId;
        public readonly bool Success;
        public readonly bool AlreadyApplied;
        public readonly int PreviousBalance;
        public readonly int CurrentBalance;
        public readonly int Delta;
        public readonly string Error;

        public CurrencyTransactionProcessedPayload(
            string transactionId,
            bool success,
            bool alreadyApplied,
            int previousBalance,
            int currentBalance,
            int delta,
            string error)
        {
            TransactionId = transactionId;
            Success = success;
            AlreadyApplied = alreadyApplied;
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            Delta = delta;
            Error = error;
        }
    }

    public readonly struct CurrencyChangedPayload
    {
        public readonly string TransactionId;
        public readonly int PreviousBalance;
        public readonly int CurrentBalance;
        public readonly int Delta;
        public readonly string Reason;

        public CurrencyChangedPayload(
            string transactionId,
            int previousBalance,
            int currentBalance,
            int delta,
            string reason)
        {
            TransactionId = transactionId;
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            Delta = delta;
            Reason = reason;
        }
    }

    /// <summary>
    /// Published only after a new Credit transaction has been persisted.
    /// Balance overrides, initialization and debit transactions never emit it.
    /// </summary>
    public readonly struct CurrencyCreditedPayload
    {
        public readonly string TransactionId;
        public readonly int Amount;
        public readonly string Reason;

        public CurrencyCreditedPayload(
            string transactionId,
            int amount,
            string reason)
        {
            TransactionId = transactionId;
            Amount = amount;
            Reason = reason;
        }
    }
}
