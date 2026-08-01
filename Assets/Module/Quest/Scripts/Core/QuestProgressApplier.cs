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
            if (amount <= 0) return false;

            if (!string.IsNullOrWhiteSpace(progressKey)
                && progress.CountedProgressKeys.Contains(progressKey))
                return false;

            if (!string.IsNullOrWhiteSpace(progressKey))
                progress.CountedProgressKeys.Add(progressKey);

            int safeRequired = Math.Max(1, requiredAmount);
            progress.currentAmount = Math.Min(safeRequired, progress.currentAmount + amount);
            progress.isCompleted = progress.currentAmount >= safeRequired;
            return true;
        }
    }
}
