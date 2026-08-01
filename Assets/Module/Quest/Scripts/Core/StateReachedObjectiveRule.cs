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
            if (progressEvent.EventType != objective.eventType) return false;
            if (!QuestTargetMatcher.Matches(objective, progressEvent)) return false;

            bool stateMatches = string.Equals(
                objective.targetState,
                progressEvent.State,
                StringComparison.Ordinal);

            if (!stateMatches) return false;

            return _progressApplier.TryAddProgress(
                progress,
                objective.requiredAmount,
                Math.Max(1, progressEvent.Amount),
                progressEvent.ProgressKey);
        }
    }
}
