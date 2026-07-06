namespace Core.Module.Quest
{
    public readonly struct QuestProgressChangedPayload
    {
        public readonly string QuestId;
        public readonly string ObjectiveId;
        public readonly int CurrentAmount;
        public readonly int RequiredAmount;
        public readonly bool IsCompleted;

        public QuestProgressChangedPayload(
            string questId,
            string objectiveId,
            int currentAmount,
            int requiredAmount,
            bool isCompleted)
        {
            QuestId = questId;
            ObjectiveId = objectiveId;
            CurrentAmount = currentAmount;
            RequiredAmount = requiredAmount;
            IsCompleted = isCompleted;
        }
    }
}
