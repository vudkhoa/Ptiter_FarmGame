namespace Core.Module.Cutscene
{
    public readonly struct CutsceneStepChangedPayload
    {
        public readonly string CutsceneId;
        public readonly int StepIndex;
        public readonly string StepId;

        public CutsceneStepChangedPayload(string cutsceneId, int stepIndex, string stepId)
        {
            CutsceneId = cutsceneId;
            StepIndex = stepIndex;
            StepId = stepId;
            // StepId and stepIndex => define one step in cutscene.
        }
    }
}
