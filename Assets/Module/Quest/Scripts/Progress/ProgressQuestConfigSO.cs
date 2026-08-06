using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [Serializable]
    public sealed class ProgressMilestoneDefinition
    {
        public string milestoneId;
        [Min(1)] public int requiredCoins = 1000;
        [Min(1)] public int starReward = 10;
    }

    [CreateAssetMenu(
        fileName = "ProgressQuestConfig",
        menuName = "GDD/Quest/Progress Quest Config")]
    public sealed class ProgressQuestConfigSO : ScriptableObject
    {
        public List<ProgressMilestoneDefinition> milestones =
            new List<ProgressMilestoneDefinition>();
    }
}
