using System;

namespace Core.Module.Quest
{
    public sealed class QuestCompletionEvaluator
    {
        public bool IsComplete(QuestDefinitionSO definition, QuestRuntimeState state)
        {
            if (definition == null || state == null) return false;
            if (definition.objectives == null || definition.objectives.Count == 0) return false;

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                var objective = definition.objectives[i];
                if (objective == null || string.IsNullOrWhiteSpace(objective.objectiveId))
                    return false;

                if (!state.TryGetProgress(objective.objectiveId, out var progress))
                    return false;

                int requiredAmount = Math.Max(1, objective.requiredAmount);
                if (!progress.isCompleted || progress.currentAmount < requiredAmount)
                    return false;
            }

            return true;
        }
    }
}
