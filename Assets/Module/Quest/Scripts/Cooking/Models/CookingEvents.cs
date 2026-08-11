namespace Core.Module.Quest.Cooking
{
    public readonly struct CookingStateChangedPayload
    {
        public string RecipeId { get; }

        public CookingStateChangedPayload(string recipeId)
        {
            RecipeId = recipeId;
        }
    }

    public readonly struct CookingCompletedPayload
    {
        public string TransactionId { get; }
        public string RecipeId { get; }
        public string OutputItemId { get; }
        public int Amount { get; }
        public int Quantity { get; }

        public CookingCompletedPayload(
            string transactionId,
            string recipeId,
            string outputItemId,
            int amount,
            int quantity)
        {
            TransactionId = transactionId;
            RecipeId = recipeId;
            OutputItemId = outputItemId;
            Amount = amount;
            Quantity = quantity;
        }
    }

    public readonly struct CookingCompletionCommittedPayload
    {
        public string TransactionId { get; }
        public string RecipeId { get; }

        public CookingCompletionCommittedPayload(
            string transactionId,
            string recipeId)
        {
            TransactionId = transactionId;
            RecipeId = recipeId;
        }
    }
}
