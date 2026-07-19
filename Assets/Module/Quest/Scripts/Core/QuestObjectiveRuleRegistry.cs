using System.Collections.Generic;

namespace Core.Module.Quest
{
    public sealed class QuestObjectiveRuleRegistry
    {
        private readonly Dictionary<QuestObjectiveType, IQuestObjectiveRule> _rulesByType;

        public QuestObjectiveRuleRegistry(IEnumerable<IQuestObjectiveRule> rules)
        {
            _rulesByType = new Dictionary<QuestObjectiveType, IQuestObjectiveRule>();
            if (rules == null) return;

            foreach (var rule in rules)
            {
                if (rule == null) continue;
                _rulesByType[rule.ObjectiveType] = rule;
            }
        }

        public bool TryGetRule(QuestObjectiveType objectiveType, out IQuestObjectiveRule rule)
        {
            return _rulesByType.TryGetValue(objectiveType, out rule);
        }
    }
}
