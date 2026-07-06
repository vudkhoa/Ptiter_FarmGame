using System;

namespace Core.Module.Quest
{
    public sealed class QuestProgressApplier
    {
        public bool TryAddProgress(
            QuestObjectiveProgress progress,
            int requiredAmount,
            int amount,
            string progressKey)
        {
            if (progress == null || progress.isCompleted) return false;

            if (!string.IsNullOrWhiteSpace(progressKey)
                && progress.countedProgressKeys.Contains(progressKey))
                return false;

            if (!string.IsNullOrWhiteSpace(progressKey))
                progress.countedProgressKeys.Add(progressKey);

            int safeRequired = Math.Max(1, requiredAmount);
            int safeAmount = Math.Max(1, amount);
            progress.currentAmount = Math.Min(safeRequired, progress.currentAmount + safeAmount);
            progress.isCompleted = progress.currentAmount >= safeRequired;
            return true;
        }
    }
}
