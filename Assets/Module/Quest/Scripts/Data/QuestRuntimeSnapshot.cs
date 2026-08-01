using System;
using System.Collections.Generic;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class QuestRuntimeSnapshot
    {
        public string runtimeId;
        public string questDefinitionId;
        public QuestStatus status;
        public List<QuestObjectiveProgressSnapshot> objectives = new List<QuestObjectiveProgressSnapshot>();
    }

    [Serializable]
    public sealed class QuestObjectiveProgressSnapshot
    {
        public string objectiveId;
        public int currentAmount;
        public bool isCompleted;
        public List<string> countedProgressKeys = new List<string>();
    }
}
