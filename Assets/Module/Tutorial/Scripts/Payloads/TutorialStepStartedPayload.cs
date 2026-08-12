namespace Core.Module.Tutorial
{
    public readonly struct TutorialStepStartedPayload
    {
        public readonly string FlowId;
        public readonly string StepId;
        public readonly int StepIndex;
        public readonly int StepCount;

        public TutorialStepStartedPayload(string flowId, string stepId, int stepIndex, int stepCount)
        {
            FlowId = flowId;
            StepId = stepId;
            StepIndex = stepIndex;
            StepCount = stepCount;
        }
    }
}
