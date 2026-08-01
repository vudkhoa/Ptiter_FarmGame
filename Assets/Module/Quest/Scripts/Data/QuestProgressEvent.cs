namespace Core.Module.Quest
{
    public readonly struct QuestProgressEvent
    {
        public readonly QuestEventType EventType;
        public readonly string TargetId;
        public readonly string TargetCategory;
        public readonly string State;
        public readonly int Amount;
        public readonly string ProgressKey;

        public QuestProgressEvent(
            QuestEventType eventType,
            string targetId,
            string targetCategory,
            string state,
            int amount,
            string progressKey = null)
        {
            EventType = eventType;
            TargetId = targetId;
            TargetCategory = targetCategory;
            State = state;
            Amount = amount;
            ProgressKey = progressKey;
        }

        public QuestProgressEvent(
            QuestObjectiveType objectiveType,
            string targetId,
            string state,
            int amount,
            string progressKey = null)
            : this(
                QuestEventType.FarmStateReached,
                targetId,
                null,
                state,
                amount,
                progressKey)
        {
        }
    }
}
