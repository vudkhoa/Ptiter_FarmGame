using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Quest
{
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "GDD/Quest/Quest Catalog")]
    public sealed class QuestCatalogSO : ScriptableObject
    {
        public List<QuestDefinitionSO> quests = new List<QuestDefinitionSO>();

        public QuestDefinitionSO GetQuestById(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) || quests == null) return null;

            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                if (quest != null && quest.questId == questId)
                    return quest;
            }

            return null;
        }
    }
}
