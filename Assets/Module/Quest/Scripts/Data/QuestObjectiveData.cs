using System;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class QuestObjectiveData
    {
        public string objectiveId;
        public QuestObjectiveType objectiveType = QuestObjectiveType.StateReached;
        public QuestEventType eventType = QuestEventType.FarmStateReached;
        public QuestTargetScope targetScope = QuestTargetScope.ExactTarget;
        public string targetId;
        public string targetCategory;
        public string targetState;
        public int requiredAmount = 1;
    }
}
