namespace Core.Module.Quest
{
    public readonly struct DailyQuestStateChangedPayload
    {
        public readonly string DayKey;

        public DailyQuestStateChangedPayload(string dayKey)
        {
            DayKey = dayKey;
        }
    }

    public readonly struct QuestRewardGrantedPayload
    {
        public readonly string TransactionId;
        public readonly DailyRewardKind Kind;
        public readonly int Coins;
        public readonly bool ReconciledAtStartup;

        public QuestRewardGrantedPayload(
            string transactionId,
            DailyRewardKind kind,
            int coins,
            bool reconciledAtStartup)
        {
            TransactionId = transactionId;
            Kind = kind;
            Coins = coins;
            ReconciledAtStartup = reconciledAtStartup;
        }
    }
}
