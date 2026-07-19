namespace Core.Module.Quest
{
    public readonly struct QuestCompletedPayload
    {
        public readonly string QuestId;

        public QuestCompletedPayload(string questId)
        {
            QuestId = questId;
        }
    }
}
