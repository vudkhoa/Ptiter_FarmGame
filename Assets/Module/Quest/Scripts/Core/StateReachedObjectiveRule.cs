using System;

namespace Core.Module.Quest
{
    public sealed class StateReachedObjectiveRule : IQuestObjectiveRule
    {
        private readonly QuestProgressApplier _progressApplier;

        public QuestObjectiveType ObjectiveType => QuestObjectiveType.StateReached;

        public StateReachedObjectiveRule(QuestProgressApplier progressApplier)
        {
            _progressApplier = progressApplier;
        }

        public bool TryApply(
            QuestObjectiveData objective,
            QuestObjectiveProgress progress,
            QuestProgressEvent progressEvent)
        {
            if (objective == null || progress == null) return false;
            if (progressEvent.EventType != ObjectiveType) return false;

            bool targetMatches = string.Equals(
                objective.targetId,
                progressEvent.TargetId,
                StringComparison.Ordinal);

            bool stateMatches = string.Equals(
                objective.targetState,
                progressEvent.State,
                StringComparison.Ordinal);

            if (!targetMatches || !stateMatches) return false;

            return _progressApplier.TryAddProgress(
                progress,
                objective.requiredAmount,
                progressEvent.Amount,
                progressEvent.ProgressKey);
        }
    }
}
