namespace Core.Module.Quest
{
    public readonly struct ProgressCoinsEarnedPayload
    {
        public readonly string TransactionId;
        public readonly int Amount;

        public ProgressCoinsEarnedPayload(string transactionId, int amount)
        {
            TransactionId = transactionId;
            Amount = amount;
        }
    }

    public readonly struct ProgressQuestStateChangedPayload
    {
        public readonly int AccumulatedCoins;
        public readonly int Stars;

        public ProgressQuestStateChangedPayload(int accumulatedCoins, int stars)
        {
            AccumulatedCoins = accumulatedCoins;
            Stars = stars;
        }
    }

    public readonly struct ProgressRewardClaimedPayload
    {
        public readonly string MilestoneId;
        public readonly int Stars;

        public ProgressRewardClaimedPayload(string milestoneId, int stars)
        {
            MilestoneId = milestoneId;
            Stars = stars;
        }
    }
}
