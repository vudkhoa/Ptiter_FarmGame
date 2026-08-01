using System.Collections.Generic;

namespace Core.Module.Quest
{
    public sealed class QuestRuntimeState
    {
        private readonly Dictionary<string, QuestObjectiveProgress> _progressByObjectiveId;

        public string RuntimeId { get; }
        public string QuestDefinitionId { get; }
        public QuestStatus Status { get; set; }
        public IReadOnlyList<QuestObjectiveProgress> ObjectiveProgress => _objectiveProgress;

        private readonly List<QuestObjectiveProgress> _objectiveProgress;

        public QuestRuntimeState(string runtimeId, QuestDefinitionSO definition)
        {
            RuntimeId = runtimeId;
            QuestDefinitionId = definition.questId;
            Status = QuestStatus.Active;
            _objectiveProgress = new List<QuestObjectiveProgress>();
            _progressByObjectiveId = new Dictionary<string, QuestObjectiveProgress>();

            if (definition.objectives == null) return;

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                var objective = definition.objectives[i];
                if (objective == null || string.IsNullOrWhiteSpace(objective.objectiveId)) continue;

                var progress = new QuestObjectiveProgress(objective.objectiveId);
                _objectiveProgress.Add(progress);
                _progressByObjectiveId[objective.objectiveId] = progress;
            }
        }

        public bool TryGetProgress(string objectiveId, out QuestObjectiveProgress progress)
        {
            if (string.IsNullOrWhiteSpace(objectiveId))
            {
                progress = null;
                return false;
            }

            return _progressByObjectiveId.TryGetValue(objectiveId, out progress);
        }

        public QuestRuntimeSnapshot CreateSnapshot()
        {
            var snapshot = new QuestRuntimeSnapshot
            {
                runtimeId = RuntimeId,
                questDefinitionId = QuestDefinitionId,
                status = Status,
                objectives = new List<QuestObjectiveProgressSnapshot>()
            };

            for (int i = 0; i < _objectiveProgress.Count; i++)
            {
                QuestObjectiveProgress progress = _objectiveProgress[i];
                snapshot.objectives.Add(new QuestObjectiveProgressSnapshot
                {
                    objectiveId = progress.objectiveId,
                    currentAmount = progress.currentAmount,
                    isCompleted = progress.isCompleted,
                    countedProgressKeys = new List<string>(progress.CountedProgressKeys)
                });
            }

            return snapshot;
        }

        public void Restore(QuestRuntimeSnapshot snapshot)
        {
            if (snapshot == null) return;
            Status = snapshot.status;
            if (snapshot.objectives == null) return;

            for (int i = 0; i < snapshot.objectives.Count; i++)
            {
                QuestObjectiveProgressSnapshot saved = snapshot.objectives[i];
                if (saved == null || !TryGetProgress(saved.objectiveId, out QuestObjectiveProgress progress))
                    continue;

                progress.currentAmount = saved.currentAmount;
                progress.isCompleted = saved.isCompleted;
                progress.RestoreProgressKeys(saved.countedProgressKeys);
            }
        }
    }
}
