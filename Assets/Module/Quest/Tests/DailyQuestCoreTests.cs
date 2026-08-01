using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Core.Module.Quest.Tests
{
    public sealed class DailyQuestCoreTests
    {
        private sealed class NullPublisher<T> : IPublisher<T>
        {
            public void Publish(T message) { }
        }

        [Test]
        public void ActionCount_UsesEventCount_NotEventAmount()
        {
            QuestService service = Service(Definition(
                "plant",
                QuestObjectiveType.ActionCount,
                QuestEventType.FarmPlanted,
                QuestTargetScope.TargetCategory,
                null,
                "Crop",
                2));
            service.ActivateQuest("daily:day:plant", "plant");

            service.ReportEvent(new QuestProgressEvent(
                QuestEventType.FarmPlanted, "c_wheat", "Crop", null, 99, "event-1"));

            QuestRuntimeState state = service.GetQuestState("daily:day:plant");
            Assert.IsTrue(state.TryGetProgress("objective", out QuestObjectiveProgress progress));
            Assert.AreEqual(1, progress.currentAmount);
            Assert.AreEqual(QuestStatus.Active, state.Status);
        }

        [Test]
        public void ItemAmount_SumsOutputQuantity_AndDeduplicatesProgressKey()
        {
            QuestService service = Service(Definition(
                "wheat",
                QuestObjectiveType.ItemAmount,
                QuestEventType.FarmHarvestItem,
                QuestTargetScope.ExactTarget,
                "wheat_grain",
                null,
                10));
            service.ActivateQuest("daily:day:wheat", "wheat");
            var harvest = new QuestProgressEvent(
                QuestEventType.FarmHarvestItem,
                "wheat_grain",
                "Item",
                null,
                6,
                "harvest-1");

            Assert.IsTrue(service.ReportEvent(harvest));
            Assert.IsFalse(service.ReportEvent(harvest));
            service.ReportEvent(new QuestProgressEvent(
                QuestEventType.FarmHarvestItem,
                "wheat_grain",
                "Item",
                null,
                4,
                "harvest-2"));

            QuestRuntimeState state = service.GetQuestState("daily:day:wheat");
            Assert.AreEqual(QuestStatus.Completed, state.Status);
            Assert.IsTrue(state.TryGetProgress("objective", out QuestObjectiveProgress progress));
            Assert.AreEqual(10, progress.currentAmount);
        }

        [Test]
        public void Snapshot_RestoresPartialProgressAndDedupeKeys()
        {
            QuestDefinitionSO definition = Definition(
                "care",
                QuestObjectiveType.ActionCount,
                QuestEventType.FarmCared,
                QuestTargetScope.Any,
                null,
                null,
                2);
            QuestService first = Service(definition);
            first.ActivateQuest("daily:day:care", "care");
            QuestProgressEvent cared = new QuestProgressEvent(
                QuestEventType.FarmCared, "a_chicken", "Animal", null, 1, "care-1");
            first.ReportEvent(cared);
            QuestRuntimeSnapshot snapshot = first.CreateSnapshot("daily:day:care");

            QuestService restored = Service(definition);
            restored.ActivateQuest("daily:day:care", "care", snapshot);
            Assert.IsFalse(restored.ReportEvent(cared));
            Assert.IsTrue(restored.ReportEvent(new QuestProgressEvent(
                QuestEventType.FarmCared,
                "a_chicken",
                "Animal",
                null,
                1,
                "care-2")));
            Assert.AreEqual(
                QuestStatus.Completed,
                restored.GetQuestState("daily:day:care").Status);
        }

        private static QuestService Service(QuestDefinitionSO definition)
        {
            QuestCatalogSO catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.quests.Add(definition);
            var applier = new QuestProgressApplier();
            var rules = new IQuestObjectiveRule[]
            {
                new StateReachedObjectiveRule(applier),
                new ActionCountObjectiveRule(applier),
                new ItemAmountObjectiveRule(applier)
            };
            return new QuestService(
                catalog,
                new QuestObjectiveRuleRegistry(rules),
                new QuestCompletionEvaluator(),
                new NullPublisher<QuestAcceptedPayload>(),
                new NullPublisher<QuestProgressChangedPayload>(),
                new NullPublisher<QuestCompletedPayload>());
        }

        private static QuestDefinitionSO Definition(
            string id,
            QuestObjectiveType objectiveType,
            QuestEventType eventType,
            QuestTargetScope targetScope,
            string targetId,
            string targetCategory,
            int required)
        {
            QuestDefinitionSO definition =
                ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.questId = id;
            definition.objectives = new List<QuestObjectiveData>
            {
                new QuestObjectiveData
                {
                    objectiveId = "objective",
                    objectiveType = objectiveType,
                    eventType = eventType,
                    targetScope = targetScope,
                    targetId = targetId,
                    targetCategory = targetCategory,
                    requiredAmount = required
                }
            };
            return definition;
        }
    }
}
