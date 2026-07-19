using System;
using System.Collections.Generic;
using MessagePipe;

namespace Core.Module.Quest
{
    public sealed class QuestService : IQuestService
    {
        private readonly QuestCatalogSO _catalog;
        private readonly QuestObjectiveRuleRegistry _ruleRegistry;
        private readonly QuestCompletionEvaluator _completionEvaluator;
        private readonly IPublisher<QuestAcceptedPayload> _acceptedPublisher;
        private readonly IPublisher<QuestProgressChangedPayload> _progressPublisher;
        private readonly IPublisher<QuestCompletedPayload> _completedPublisher;

        private readonly Dictionary<string, QuestRuntimeState> _statesByQuestId = new Dictionary<string, QuestRuntimeState>();
        private readonly List<QuestRuntimeState> _activeQuests = new List<QuestRuntimeState>();

        public IReadOnlyList<QuestRuntimeState> ActiveQuests => _activeQuests;

        public QuestService(
            QuestCatalogSO catalog,
            QuestObjectiveRuleRegistry ruleRegistry,
            QuestCompletionEvaluator completionEvaluator,
            IPublisher<QuestAcceptedPayload> acceptedPublisher,
            IPublisher<QuestProgressChangedPayload> progressPublisher,
            IPublisher<QuestCompletedPayload> completedPublisher)
        {
            _catalog = catalog;
            _ruleRegistry = ruleRegistry;
            _completionEvaluator = completionEvaluator;
            _acceptedPublisher = acceptedPublisher;
            _progressPublisher = progressPublisher;
            _completedPublisher = completedPublisher;
        }

        public bool AcceptQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return false;
            if (_statesByQuestId.ContainsKey(questId)) return false;

            var definition = _catalog != null ? _catalog.GetQuestById(questId) : null;
            if (definition == null) return false;

            var state = new QuestRuntimeState(definition);
            _statesByQuestId.Add(questId, state);
            _activeQuests.Add(state);

            _acceptedPublisher?.Publish(new QuestAcceptedPayload(questId));
            return true;
        }

        public bool ReportEvent(QuestProgressEvent progressEvent)
        {
            bool anyChanged = false;

            for (int questIndex = _activeQuests.Count - 1; questIndex >= 0; questIndex--)
            {
                var state = _activeQuests[questIndex];
                if (state == null || state.Status != QuestStatus.Active) continue;

                var definition = _catalog != null ? _catalog.GetQuestById(state.QuestId) : null;
                if (definition == null || definition.objectives == null) continue;

                bool questChanged = false;

                for (int objectiveIndex = 0; objectiveIndex < definition.objectives.Count; objectiveIndex++)
                {
                    var objective = definition.objectives[objectiveIndex];
                    if (objective == null) continue;
                    if (!state.TryGetProgress(objective.objectiveId, out var progress)) continue;
                    if (!_ruleRegistry.TryGetRule(objective.objectiveType, out var rule)) continue;

                    bool changed = rule.TryApply(objective, progress, progressEvent);
                    if (!changed) continue;

                    questChanged = true;
                    anyChanged = true;
                    _progressPublisher?.Publish(new QuestProgressChangedPayload(
                        state.QuestId,
                        objective.objectiveId,
                        progress.currentAmount,
                        Math.Max(1, objective.requiredAmount),
                        progress.isCompleted));
                }

                if (questChanged && _completionEvaluator.IsComplete(definition, state))
                {
                    state.Status = QuestStatus.Completed;
                    _activeQuests.RemoveAt(questIndex);
                    _completedPublisher?.Publish(new QuestCompletedPayload(state.QuestId));
                }
            }

            return anyChanged;
        }

        public QuestRuntimeState GetQuestState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return null;
            return _statesByQuestId.TryGetValue(questId, out var state) ? state : null;
        }
    }
}
