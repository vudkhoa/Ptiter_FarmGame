using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "NewCutscene", menuName = "GDD/Cutscene/Cutscene")]
    public sealed class CutsceneSO : ScriptableObject
    {
        public string cutsceneId;
        [TextArea] public string description;
        public bool allowSkip = true;
        public List<CutsceneStepSO> steps = new List<CutsceneStepSO>();
    }
}
