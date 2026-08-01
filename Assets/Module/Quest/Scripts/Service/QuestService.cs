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

        private readonly Dictionary<string, QuestRuntimeState> _statesByRuntimeId = new Dictionary<string, QuestRuntimeState>();
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

        public bool ActivateQuest(
            string runtimeId,
            string questDefinitionId,
            QuestRuntimeSnapshot snapshot = null)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) ||
                string.IsNullOrWhiteSpace(questDefinitionId) ||
                _statesByRuntimeId.ContainsKey(runtimeId))
                return false;

            var definition = _catalog != null ? _catalog.GetQuestById(questDefinitionId) : null;
            if (definition == null) return false;

            var state = new QuestRuntimeState(runtimeId, definition);
            state.Restore(snapshot);
            if (state.Status == QuestStatus.Active &&
                _completionEvaluator.IsComplete(definition, state))
                state.Status = QuestStatus.Completed;
            _statesByRuntimeId.Add(runtimeId, state);
            if (state.Status == QuestStatus.Active)
                _activeQuests.Add(state);

            _acceptedPublisher?.Publish(new QuestAcceptedPayload(runtimeId, questDefinitionId));
            return true;
        }

        public bool AcceptQuest(string questDefinitionId)
        {
            return ActivateQuest(questDefinitionId, questDefinitionId);
        }

        public bool DeactivateQuest(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) ||
                !_statesByRuntimeId.TryGetValue(runtimeId, out QuestRuntimeState state))
                return false;

            _activeQuests.Remove(state);
            _statesByRuntimeId.Remove(runtimeId);
            return true;
        }

        public int DeactivateQuests(IEnumerable<string> runtimeIds)
        {
            if (runtimeIds == null) return 0;
            int count = 0;
            foreach (string runtimeId in runtimeIds)
            {
                if (DeactivateQuest(runtimeId))
                    count++;
            }
            return count;
        }

        public bool ReportEvent(QuestProgressEvent progressEvent)
        {
            bool anyChanged = false;

            for (int questIndex = _activeQuests.Count - 1; questIndex >= 0; questIndex--)
            {
                var state = _activeQuests[questIndex];
                if (state == null || state.Status != QuestStatus.Active) continue;

                var definition = _catalog != null ? _catalog.GetQuestById(state.QuestDefinitionId) : null;
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
                        state.RuntimeId,
                        state.QuestDefinitionId,
                        objective.objectiveId,
                        progress.currentAmount,
                        Math.Max(1, objective.requiredAmount),
                        progress.isCompleted));
                }

                if (questChanged && _completionEvaluator.IsComplete(definition, state))
                {
                    state.Status = QuestStatus.Completed;
                    _activeQuests.RemoveAt(questIndex);
                    _completedPublisher?.Publish(new QuestCompletedPayload(
                        state.RuntimeId,
                        state.QuestDefinitionId));
                }
            }

            return anyChanged;
        }

        public QuestRuntimeState GetQuestState(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) return null;
            return _statesByRuntimeId.TryGetValue(runtimeId, out var state) ? state : null;
        }

        public QuestRuntimeSnapshot CreateSnapshot(string runtimeId)
        {
            QuestRuntimeState state = GetQuestState(runtimeId);
            return state?.CreateSnapshot();
        }
    }
}
