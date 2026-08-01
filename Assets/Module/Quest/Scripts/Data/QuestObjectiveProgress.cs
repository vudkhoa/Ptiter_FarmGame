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

        [NonSerialized]
        private HashSet<string> _countedProgressKeys = new HashSet<string>();

        public HashSet<string> CountedProgressKeys => _countedProgressKeys ??= new HashSet<string>();

        public QuestObjectiveProgress(string objectiveId)
        {
            this.objectiveId = objectiveId;
        }

        public void RestoreProgressKeys(IEnumerable<string> keys)
        {
            _countedProgressKeys = keys == null
                ? new HashSet<string>()
                : new HashSet<string>(keys);
        }
    }
}
