namespace Core.Module.Quest
{
    public interface IQuestObjectiveRule
    {
        QuestObjectiveType ObjectiveType { get; }

        bool TryApply(
            QuestObjectiveData objective,
            QuestObjectiveProgress progress,
            QuestProgressEvent progressEvent);
    }
}
