using System;
using System.Collections.Generic;

namespace Core.Module.Quest
{
    public enum DailyRewardKind
    {
        Task = 0,
        Milestone = 1
    }

    public enum DailyMilestoneClaimState
    {
        Locked = 0,
        Claimable = 1,
        ClaimPending = 2,
        Claimed = 3
    }

    [Serializable]
    public sealed class DailyQuestTaskSaveData
    {
        public string runtimeId;
        public string questDefinitionId;
        public int points;
        public int coinReward;
        public bool rewardQueued;
        public QuestRuntimeSnapshot quest;
    }

    [Serializable]
    public sealed class DailyMilestoneSaveData
    {
        public string milestoneId;
        public int requiredPoints;
        public int coinReward;
        public bool claimed;
    }

    [Serializable]
    public sealed class DailyQuestSaveData
    {
        public string dayKey;
        public string setId;
        public List<DailyQuestTaskSaveData> tasks = new List<DailyQuestTaskSaveData>();
        public List<DailyMilestoneSaveData> milestones = new List<DailyMilestoneSaveData>();
    }

    [Serializable]
    public sealed class PendingQuestRewardSaveData
    {
        public string transactionId;
        public string dayKey;
        public string sourceId;
        public DailyRewardKind kind;
        public int coins;
    }

    public sealed class DailyQuestTaskViewData
    {
        public string RuntimeId { get; internal set; }
        public string DefinitionId { get; internal set; }
        public string Title { get; internal set; }
        public string Description { get; internal set; }
        public UnityEngine.Sprite Icon { get; internal set; }
        public int CurrentAmount { get; internal set; }
        public int RequiredAmount { get; internal set; }
        public int CoinReward { get; internal set; }
        public bool IsCompleted { get; internal set; }
        public bool IsRewardPending { get; internal set; }
    }

    public sealed class DailyMilestoneViewData
    {
        public string MilestoneId { get; internal set; }
        public int RequiredPoints { get; internal set; }
        public int CoinReward { get; internal set; }
        public DailyMilestoneClaimState ClaimState { get; internal set; }
    }

    public sealed class DailyQuestViewState
    {
        public bool IsReady { get; internal set; }
        public string LockedReason { get; internal set; }
        public string DayKey { get; internal set; }
        public int TotalPoints { get; internal set; }
        public TimeSpan TimeUntilReset { get; internal set; }
        public IReadOnlyList<DailyQuestTaskViewData> Tasks { get; internal set; }
        public IReadOnlyList<DailyMilestoneViewData> Milestones { get; internal set; }
    }
}
