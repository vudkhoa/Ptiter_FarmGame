using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// The single authored entry point for the tutorial. Flows run strictly in list order:
    /// flow N+1 is never considered until flow N is complete, even if its start signal fires early.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TutorialCatalog",
        menuName = "Game/Tutorial/Tutorial Catalog",
        order = 0)]
    public sealed class TutorialCatalogSO : ScriptableObject
    {
        [Tooltip("Master switch. Off disables every flow without touching the save file.")]
        public bool tutorialEnabled = true;

        [Tooltip("Mark all flows complete for players whose save predates the tutorial.")]
        public bool skipForExistingSaves = true;

        public List<TutorialFlowSO> flows = new List<TutorialFlowSO>();

        /// <summary>First flow, in authoring order, that is not yet recorded as complete.</summary>
        public TutorialFlowSO GetFirstIncompleteFlow(TutorialSaveData progress)
        {
            if (flows == null || progress == null) return null;

            for (int i = 0; i < flows.Count; i++)
            {
                TutorialFlowSO flow = flows[i];
                if (flow == null || !flow.IsValid) continue;
                if (!progress.IsFlowCompleted(flow.flowId)) return flow;
            }

            return null;
        }
    }
}
