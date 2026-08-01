namespace Core.Module.Quest
{
    public sealed class ItemAmountObjectiveRule : IQuestObjectiveRule
    {
        private readonly QuestProgressApplier _progressApplier;

        public QuestObjectiveType ObjectiveType => QuestObjectiveType.ItemAmount;

        public ItemAmountObjectiveRule(QuestProgressApplier progressApplier)
        {
            _progressApplier = progressApplier;
        }

        public bool TryApply(
            QuestObjectiveData objective,
            QuestObjectiveProgress progress,
            QuestProgressEvent progressEvent)
        {
            if (objective == null || progress == null || progressEvent.Amount <= 0) return false;
            if (objective.eventType != progressEvent.EventType) return false;
            if (!QuestTargetMatcher.Matches(objective, progressEvent)) return false;

            return _progressApplier.TryAddProgress(
                progress,
                objective.requiredAmount,
                progressEvent.Amount,
                progressEvent.ProgressKey);
        }
    }
}
