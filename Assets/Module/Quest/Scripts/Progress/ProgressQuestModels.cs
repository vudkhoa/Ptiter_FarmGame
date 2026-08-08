using System;
using System.Collections.Generic;

namespace Core.Module.Quest
{
    public enum ProgressMilestoneClaimState
    {
        Locked = 0,
        Claimable = 1,
        Claimed = 2
    }

    [Serializable]
    public sealed class ProgressQuestSaveData
    {
        public int accumulatedCoins;
        public int stars;
        public List<string> claimedMilestoneIds = new List<string>();
    }

    public sealed class ProgressMilestoneViewData
    {
        public string MilestoneId { get; internal set; }
        public int CurrentCoins { get; internal set; }
        public int RequiredCoins { get; internal set; }
        public int StarReward { get; internal set; }
        public ProgressMilestoneClaimState ClaimState { get; internal set; }
    }

    public sealed class ProgressQuestViewState
    {
        public bool IsReady { get; internal set; }
        public string LockedReason { get; internal set; }
        public int AccumulatedCoins { get; internal set; }
        public int Stars { get; internal set; }
        public IReadOnlyList<ProgressMilestoneViewData> Milestones { get; internal set; }
    }
}
