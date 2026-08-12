using System;
using System.Collections.Generic;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Persisted tutorial progress. Only completed ids are stored: the flow always
    /// resumes at the first step of the first flow whose id is not in these lists,
    /// so adding a step to an existing flow replays only that new step.
    /// </summary>
    [Serializable]
    public sealed class TutorialSaveData
    {
        /// <summary>
        /// False until the service has looked at this save once. It is what tells a save
        /// created before the tutorial existed apart from a brand new player, so returning
        /// veterans never get the hand pointing at land they placed months ago.
        /// </summary>
        public bool initialized;

        public List<string> completedStepIds = new List<string>();
        public List<string> completedFlowIds = new List<string>();

        public bool IsStepCompleted(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || completedStepIds == null) return false;
            for (int i = 0; i < completedStepIds.Count; i++)
                if (completedStepIds[i] == stepId) return true;
            return false;
        }

        public bool IsFlowCompleted(string flowId)
        {
            if (string.IsNullOrEmpty(flowId) || completedFlowIds == null) return false;
            for (int i = 0; i < completedFlowIds.Count; i++)
                if (completedFlowIds[i] == flowId) return true;
            return false;
        }

        public bool MarkStepCompleted(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return false;
            completedStepIds ??= new List<string>();
            if (IsStepCompleted(stepId)) return false;
            completedStepIds.Add(stepId);
            return true;
        }

        public bool MarkFlowCompleted(string flowId)
        {
            if (string.IsNullOrEmpty(flowId)) return false;
            completedFlowIds ??= new List<string>();
            if (IsFlowCompleted(flowId)) return false;
            completedFlowIds.Add(flowId);
            return true;
        }
    }
}
