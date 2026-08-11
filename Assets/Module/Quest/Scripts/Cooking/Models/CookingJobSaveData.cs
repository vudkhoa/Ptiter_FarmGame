using System;

namespace Core.Module.Quest.Cooking
{
    [Serializable]
    public sealed class CookingJobSaveData
    {
        public string transactionId;
        public string recipeId;
        public string outputItemId;
        public int outputAmount;
        public int quantity;
        public int totalSeconds;
        public long endsAtUtcTicks;
        public bool completionPending;
    }
}
