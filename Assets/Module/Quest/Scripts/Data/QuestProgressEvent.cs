using System;

namespace Core.Module.Quest
{
    public readonly struct QuestProgressEvent
    {
        public readonly QuestObjectiveType EventType;
        public readonly string TargetId;
        public readonly string State;
        public readonly int Amount;
        public readonly string ProgressKey;

        public QuestProgressEvent(
            QuestObjectiveType eventType,
            string targetId,
            string state,
            int amount = 1,
            string progressKey = null)
        {
            EventType = eventType;
            TargetId = targetId;
            State = state;
            Amount = Math.Max(1, amount);
            ProgressKey = progressKey;
        }
    }
}
