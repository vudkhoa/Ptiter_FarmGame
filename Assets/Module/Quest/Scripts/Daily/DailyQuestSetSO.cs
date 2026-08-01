using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class DailyQuestEntry
    {
        public QuestDefinitionSO quest;
        [Min(0)] public int points = 25;
        [Min(0)] public int coinReward = 100;
    }

    [Serializable]
    public sealed class DailyMilestoneDefinition
    {
        public string milestoneId;
        [Min(0)] public int requiredPoints;
        [Min(0)] public int coinReward;
    }

    [CreateAssetMenu(fileName = "DailyQuestSet", menuName = "GDD/Quest/Daily Quest Set")]
    public sealed class DailyQuestSetSO : ScriptableObject
    {
        public string setId;
        public List<DailyQuestEntry> quests = new List<DailyQuestEntry>();
        public List<DailyMilestoneDefinition> milestones = new List<DailyMilestoneDefinition>();
    }
}
