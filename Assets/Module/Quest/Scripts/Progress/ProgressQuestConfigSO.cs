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

        private void OnValidate()
        {
            if (milestones == null) return;

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < milestones.Count; i++)
            {
                ProgressMilestoneDefinition milestone = milestones[i];
                if (milestone == null) continue;

                string id = milestone.milestoneId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning(
                        $"[ProgressQuest] Milestone at index {i} has an empty ID and will be ignored.",
                        this);
                    continue;
                }

                if (!usedIds.Add(id))
                {
                    Debug.LogWarning(
                        $"[ProgressQuest] Duplicate milestone ID '{id}' at index {i} will be ignored.",
                        this);
                }
            }
        }
    }
}
