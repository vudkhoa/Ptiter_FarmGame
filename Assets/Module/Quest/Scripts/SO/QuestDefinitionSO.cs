using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [CreateAssetMenu(fileName = "NewQuestDefinition", menuName = "GDD/Quest/Quest Definition")]
    public sealed class QuestDefinitionSO : ScriptableObject
    {
        public string questId;
        public string questName;
        [TextArea] public string description;
        public List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();
    }
}
