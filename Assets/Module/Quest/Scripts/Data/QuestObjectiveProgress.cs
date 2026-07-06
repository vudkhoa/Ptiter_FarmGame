using System;
using System.Collections.Generic;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class QuestObjectiveProgress
    {
        public string objectiveId;
        public int currentAmount;
        public bool isCompleted;
        public HashSet<string> countedProgressKeys = new HashSet<string>();

        public QuestObjectiveProgress(string objectiveId)
        {
            this.objectiveId = objectiveId;
        }
    }
}
