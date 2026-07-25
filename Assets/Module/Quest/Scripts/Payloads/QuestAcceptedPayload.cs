namespace Core.Module.Quest
{
    public readonly struct QuestAcceptedPayload
    {
        public readonly string QuestId;

        public QuestAcceptedPayload(string questId)
        {
            QuestId = questId;
        }
    }
}
