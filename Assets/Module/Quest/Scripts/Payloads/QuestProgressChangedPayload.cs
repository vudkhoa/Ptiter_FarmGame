namespace Core.Module.Quest
{
    public readonly struct QuestProgressChangedPayload
    {
        public readonly string RuntimeId;
        public readonly string QuestDefinitionId;
        public readonly string ObjectiveId;
        public readonly int CurrentAmount;
        public readonly int RequiredAmount;
        public readonly bool IsCompleted;

        public QuestProgressChangedPayload(
            string runtimeId,
            string questDefinitionId,
            string objectiveId,
            int currentAmount,
            int requiredAmount,
            bool isCompleted)
        {
            RuntimeId = runtimeId;
            QuestDefinitionId = questDefinitionId;
            ObjectiveId = objectiveId;
            CurrentAmount = currentAmount;
            RequiredAmount = requiredAmount;
            IsCompleted = isCompleted;
        }
    }
}
