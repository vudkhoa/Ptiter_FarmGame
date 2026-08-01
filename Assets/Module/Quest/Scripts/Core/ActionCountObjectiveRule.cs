namespace Core.Module.Quest
{
    public sealed class ActionCountObjectiveRule : IQuestObjectiveRule
    {
        private readonly QuestProgressApplier _progressApplier;

        public QuestObjectiveType ObjectiveType => QuestObjectiveType.ActionCount;

        public ActionCountObjectiveRule(QuestProgressApplier progressApplier)
        {
            _progressApplier = progressApplier;
        }

        public bool TryApply(
            QuestObjectiveData objective,
            QuestObjectiveProgress progress,
            QuestProgressEvent progressEvent)
        {
            if (objective == null || progress == null) return false;
            if (objective.eventType != progressEvent.EventType) return false;
            if (!QuestTargetMatcher.Matches(objective, progressEvent)) return false;

            return _progressApplier.TryAddProgress(
                progress,
                objective.requiredAmount,
                1,
                progressEvent.ProgressKey);
        }
    }
}
