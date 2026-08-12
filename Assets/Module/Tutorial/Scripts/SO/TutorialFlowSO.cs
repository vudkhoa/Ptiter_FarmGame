using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// An ordered group of steps that share one trigger, e.g. "first farm" or "first harvest".
    /// A flow is only marked complete once every one of its steps is complete.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TutorialFlow",
        menuName = "Game/Tutorial/Tutorial Flow",
        order = 2)]
    public sealed class TutorialFlowSO : ScriptableObject
    {
        [Tooltip("Stable id persisted in the save file.")]
        public string flowId;

        [Tooltip("Signal that opens this flow. None starts it as soon as it becomes the current flow.")]
        public TutorialSignal startSignal = TutorialSignal.None;

        [Tooltip("Steps run top to bottom; already completed ones are skipped on resume.")]
        public List<TutorialStepSO> steps = new List<TutorialStepSO>();

        public bool IsValid => !string.IsNullOrWhiteSpace(flowId) && steps != null && steps.Count > 0;
    }
}
