namespace Core.Module.Quest
{
    public readonly struct QuestAcceptedPayload
    {
        public readonly string RuntimeId;
        public readonly string QuestDefinitionId;
        public string QuestId => QuestDefinitionId;

        public QuestAcceptedPayload(string runtimeId, string questDefinitionId)
        {
            RuntimeId = runtimeId;
            QuestDefinitionId = questDefinitionId;
        }
    }
}
