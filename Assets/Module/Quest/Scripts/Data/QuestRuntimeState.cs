using System.Collections.Generic;

namespace Core.Module.Quest
{
    public sealed class QuestRuntimeState
    {
        private readonly Dictionary<string, QuestObjectiveProgress> _progressByObjectiveId;

        public string QuestId { get; }
        public QuestStatus Status { get; set; }
        public IReadOnlyList<QuestObjectiveProgress> ObjectiveProgress => _objectiveProgress;

        private readonly List<QuestObjectiveProgress> _objectiveProgress;

        public QuestRuntimeState(QuestDefinitionSO definition)
        {
            QuestId = definition.questId;
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
    }
}
