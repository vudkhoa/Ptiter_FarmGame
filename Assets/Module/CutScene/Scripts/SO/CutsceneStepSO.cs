using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "NewCutsceneStep", menuName = "GDD/Cutscene/Cutscene Step")]
    public sealed class CutsceneStepSO : ScriptableObject
    {
        public string stepId;
        public CutsceneRunMode runMode = CutsceneRunMode.Parallel;
        public List<CutsceneTaskSO> tasks = new List<CutsceneTaskSO>();
    }
}
