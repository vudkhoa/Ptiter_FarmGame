namespace Core.Module.Quest.Cooking
{
    public interface ICookingJobRepository
    {
        CookingJobSaveData LoadActiveCookingJob();
        bool SaveActiveCookingJob(CookingJobSaveData job);
        bool TryCommitCookingCompletion(CookingCompletedPayload payload);
    }
}
