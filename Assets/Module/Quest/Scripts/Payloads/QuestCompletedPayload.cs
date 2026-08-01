namespace Core.Module.Quest
{
    public readonly struct QuestCompletedPayload
    {
        public readonly string RuntimeId;
        public readonly string QuestDefinitionId;
        public string QuestId => QuestDefinitionId;

        public QuestCompletedPayload(string runtimeId, string questDefinitionId)
        {
            RuntimeId = runtimeId;
            QuestDefinitionId = questDefinitionId;
        }
    }
}
