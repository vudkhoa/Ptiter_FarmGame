namespace Core.Module.Tutorial
{
    public readonly struct TutorialStepCompletedPayload
    {
        public readonly string FlowId;
        public readonly string StepId;

        public TutorialStepCompletedPayload(string flowId, string stepId)
        {
            FlowId = flowId;
            StepId = stepId;
        }
    }
}
