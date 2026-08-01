namespace Core.Module.Cutscene
{
    public readonly struct CutsceneStartedPayload
    {
        public readonly string CutsceneId;
        public readonly int TotalSteps;

        public CutsceneStartedPayload(string cutsceneId, int totalSteps)
        {
            CutsceneId = cutsceneId;
            TotalSteps = totalSteps;
        }
    }
}
