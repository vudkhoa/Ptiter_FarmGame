namespace Core.Module.Tutorial
{
    public readonly struct TutorialFlowCompletedPayload
    {
        public readonly string FlowId;

        /// <summary>True when this was the last flow in the catalog - the tutorial is over.</summary>
        public readonly bool IsLastFlow;

        public TutorialFlowCompletedPayload(string flowId, bool isLastFlow)
        {
            FlowId = flowId;
            IsLastFlow = isLastFlow;
        }
    }
}
