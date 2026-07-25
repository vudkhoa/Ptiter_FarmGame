using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace Core.Module.Quest.Tests
{
    [TestFixture]
    public sealed class QuestServiceTests
    {
        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public readonly List<T> Published = new List<T>();
            public void Publish(T message) => Published.Add(message);
        }

        private RecordingPublisher<QuestAcceptedPayload> _acceptedPublisher;
        private RecordingPublisher<QuestProgressChangedPayload> _progressPublisher;
        private RecordingPublisher<QuestCompletedPayload> _completedPublisher;

        [SetUp]
        public void SetUp()
        {
            _acceptedPublisher = new RecordingPublisher<QuestAcceptedPayload>();
            _progressPublisher = new RecordingPublisher<QuestProgressChangedPayload>();
            _completedPublisher = new RecordingPublisher<QuestCompletedPayload>();
        }

        [Test]
        public void AcceptQuest_CreatesRuntimeState_AndPublishesAccepted()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_grow",
                Objective("obj_growing", "c_wheat", "Growing"))));

            bool accepted = service.AcceptQuest("q_wheat_grow");

            Assert.IsTrue(accepted);
            Assert.AreEqual(1, service.ActiveQuests.Count);
            Assert.AreEqual(QuestStatus.Active, service.GetQuestState("q_wheat_grow").Status);
            Assert.AreEqual(1, _acceptedPublisher.Published.Count);
            Assert.AreEqual("q_wheat_grow", _acceptedPublisher.Published[0].QuestId);
        }

        [Test]
        public void AcceptQuest_ReturnsFalse_WhenQuestIdNotFound()
        {
            var service = CreateService(CreateCatalog());

            bool accepted = service.AcceptQuest("missing_quest");

            Assert.IsFalse(accepted);
            Assert.AreEqual(0, service.ActiveQuests.Count);
            Assert.AreEqual(0, _acceptedPublisher.Published.Count);
        }

        [Test]
        public void ReportEvent_DoesNotProgress_WhenQuestNotAccepted()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_ripe",
                Objective("obj_ripe", "c_wheat", "Ripe"))));

            bool changed = service.ReportEvent(Event("c_wheat", "Ripe", "0:0:0:c_wheat:Ripe"));

            Assert.IsFalse(changed);
            Assert.AreEqual(0, _progressPublisher.Published.Count);
            Assert.AreEqual(0, _completedPublisher.Published.Count);
        }

        [Test]
        public void StateReachedRule_Progresses_WhenTargetAndStateMatch()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_ripe",
                Objective("obj_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_wheat_ripe");

            bool changed = service.ReportEvent(Event("c_wheat", "Ripe", "0:0:0:c_wheat:Ripe"));
            var progress = GetProgress(service, "q_wheat_ripe", "obj_ripe");

            Assert.IsTrue(changed);
            Assert.AreEqual(1, progress.currentAmount);
            Assert.IsTrue(progress.isCompleted);
            Assert.AreEqual(1, _progressPublisher.Published.Count);
        }

        [Test]
        public void StateReachedRule_Ignores_WhenTargetMismatch()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_ripe",
                Objective("obj_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_wheat_ripe");

            bool changed = service.ReportEvent(Event("c_corn", "Ripe", "0:0:0:c_corn:Ripe"));
            var progress = GetProgress(service, "q_wheat_ripe", "obj_ripe");

            Assert.IsFalse(changed);
            Assert.AreEqual(0, progress.currentAmount);
            Assert.IsFalse(progress.isCompleted);
            Assert.AreEqual(0, _progressPublisher.Published.Count);
        }

        [Test]
        public void StateReachedRule_Ignores_WhenStateMismatch()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_ripe",
                Objective("obj_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_wheat_ripe");

            bool changed = service.ReportEvent(Event("c_wheat", "Growing", "0:0:0:c_wheat:Growing"));
            var progress = GetProgress(service, "q_wheat_ripe", "obj_ripe");

            Assert.IsFalse(changed);
            Assert.AreEqual(0, progress.currentAmount);
            Assert.IsFalse(progress.isCompleted);
            Assert.AreEqual(0, _progressPublisher.Published.Count);
        }

        [Test]
        public void StateReachedRule_DoesNotDoubleCount_SameProgressKey()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_growing",
                Objective("obj_growing", "c_wheat", "Growing", 2))));
            service.AcceptQuest("q_wheat_growing");

            var progressEvent = Event("c_wheat", "Growing", "0:0:0:c_wheat:Growing");
            bool firstChanged = service.ReportEvent(progressEvent);
            bool secondChanged = service.ReportEvent(progressEvent);
            var progress = GetProgress(service, "q_wheat_growing", "obj_growing");

            Assert.IsTrue(firstChanged);
            Assert.IsFalse(secondChanged);
            Assert.AreEqual(1, progress.currentAmount);
            Assert.IsFalse(progress.isCompleted);
            Assert.AreEqual(1, _progressPublisher.Published.Count);
        }

        [Test]
        public void QuestService_CompletesQuest_WhenAllObjectivesComplete()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_cycle",
                Objective("obj_growing", "c_wheat", "Growing"),
                Objective("obj_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_wheat_cycle");

            service.ReportEvent(Event("c_wheat", "Growing", "0:0:0:c_wheat:Growing"));
            service.ReportEvent(Event("c_wheat", "Ripe", "0:0:0:c_wheat:Ripe"));

            Assert.AreEqual(QuestStatus.Completed, service.GetQuestState("q_wheat_cycle").Status);
            Assert.AreEqual(0, service.ActiveQuests.Count);
            Assert.AreEqual(1, _completedPublisher.Published.Count);
            Assert.AreEqual("q_wheat_cycle", _completedPublisher.Published[0].QuestId);
        }

        [Test]
        public void QuestService_PublishesCompletedOnlyOnce()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_wheat_ripe",
                Objective("obj_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_wheat_ripe");

            service.ReportEvent(Event("c_wheat", "Ripe", "0:0:0:c_wheat:Ripe"));
            service.ReportEvent(Event("c_wheat", "Ripe", "0:0:0:c_wheat:Ripe"));
            service.ReportEvent(Event("c_wheat", "Ripe", "1:0:0:c_wheat:Ripe"));

            Assert.AreEqual(QuestStatus.Completed, service.GetQuestState("q_wheat_ripe").Status);
            Assert.AreEqual(1, _completedPublisher.Published.Count);
        }

        [Test]
        public void FarmLikeCropQuest_GrowingThenRipe_CompletesExpectedObjective()
        {
            var service = CreateService(CreateCatalog(CreateQuest(
                "q_first_crop",
                Objective("obj_reach_growing", "c_wheat", "Growing"),
                Objective("obj_reach_ripe", "c_wheat", "Ripe"))));
            service.AcceptQuest("q_first_crop");

            bool growingChanged = service.ReportEvent(Event("c_wheat", "Growing", "2:0:2:c_wheat:Growing"));
            bool ripeChanged = service.ReportEvent(Event("c_wheat", "Ripe", "2:0:2:c_wheat:Ripe"));

            Assert.IsTrue(growingChanged);
            Assert.IsTrue(ripeChanged);
            Assert.AreEqual(QuestStatus.Completed, service.GetQuestState("q_first_crop").Status);
            Assert.AreEqual(2, _progressPublisher.Published.Count);
            Assert.AreEqual(1, _completedPublisher.Published.Count);
        }

        private QuestService CreateService(QuestCatalogSO catalog)
        {
            var progressApplier = new QuestProgressApplier();
            var rules = new IQuestObjectiveRule[]
            {
                new StateReachedObjectiveRule(progressApplier)
            };

            return new QuestService(
                catalog,
                new QuestObjectiveRuleRegistry(rules),
                new QuestCompletionEvaluator(),
                _acceptedPublisher,
                _progressPublisher,
                _completedPublisher);
        }

        private static QuestCatalogSO CreateCatalog(params QuestDefinitionSO[] quests)
        {
            var catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            catalog.quests.AddRange(quests);
            return catalog;
        }

        private static QuestDefinitionSO CreateQuest(string questId, params QuestObjectiveData[] objectives)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            quest.questId = questId;
            quest.questName = questId;
            quest.objectives.AddRange(objectives);
            return quest;
        }

        private static QuestObjectiveData Objective(
            string objectiveId,
            string targetId,
            string targetState,
            int requiredAmount = 1)
        {
            return new QuestObjectiveData
            {
                objectiveId = objectiveId,
                objectiveType = QuestObjectiveType.StateReached,
                targetId = targetId,
                targetState = targetState,
                requiredAmount = requiredAmount
            };
        }

        private static QuestProgressEvent Event(string targetId, string state, string progressKey)
        {
            return new QuestProgressEvent(
                QuestObjectiveType.StateReached,
                targetId,
                state,
                1,
                progressKey);
        }

        private static QuestObjectiveProgress GetProgress(
            IQuestService service,
            string questId,
            string objectiveId)
        {
            var state = service.GetQuestState(questId);
            Assert.IsNotNull(state);
            Assert.IsTrue(state.TryGetProgress(objectiveId, out var progress));
            return progress;
        }
    }
}
