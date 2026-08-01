using System;

namespace Core.Module.Quest
{
    public static class QuestTargetMatcher
    {
        public static bool Matches(QuestObjectiveData objective, QuestProgressEvent progressEvent)
        {
            if (objective == null) return false;

            switch (objective.targetScope)
            {
                case QuestTargetScope.Any:
                    return true;
                case QuestTargetScope.ExactTarget:
                    return !string.IsNullOrWhiteSpace(objective.targetId)
                           && string.Equals(objective.targetId, progressEvent.TargetId, StringComparison.Ordinal);
                case QuestTargetScope.TargetCategory:
                    return !string.IsNullOrWhiteSpace(objective.targetCategory)
                           && string.Equals(
                               objective.targetCategory,
                               progressEvent.TargetCategory,
                               StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}
