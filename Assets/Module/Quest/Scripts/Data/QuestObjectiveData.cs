using System;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class QuestObjectiveData
    {
        public string objectiveId;
        public QuestObjectiveType objectiveType = QuestObjectiveType.StateReached;
        public string targetId;
        public string targetState;
        public int requiredAmount = 1;
    }
}
