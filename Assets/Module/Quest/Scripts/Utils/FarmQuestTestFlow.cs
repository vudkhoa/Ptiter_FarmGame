#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Core.Module.Farm;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Quest.Utils
{
    /// <summary>
    /// Editor/development bridge that translates farm state changes into quest events.
    /// It also accepts catalog quests automatically so the farm scene can test the full flow.
    /// </summary>
    public sealed class FarmQuestTestFlow : IStartable, IDisposable
    {
        private readonly QuestCatalogSO _catalog;
        private readonly IQuestService _questService;
        private readonly IDisposable _subscriptions;

        public FarmQuestTestFlow(
            QuestCatalogSO catalog,
            IQuestService questService,
            ISubscriber<FarmSlotChangedPayload> farmSlotChanged)
        {
            _catalog = catalog;
            _questService = questService;

            var subscriptions = DisposableBag.CreateBuilder();
            farmSlotChanged.Subscribe(OnFarmSlotChanged).AddTo(subscriptions);
            _subscriptions = subscriptions.Build();
        }

        public void Start()
        {
            if (_catalog == null || _catalog.quests == null) return;

            foreach (var quest in _catalog.quests)
            {
                if (quest != null)
                    _questService.AcceptQuest(quest.questId);
            }
        }

        private void OnFarmSlotChanged(FarmSlotChangedPayload payload)
        {
            var slot = payload.Slot;
            if (slot == null || string.IsNullOrWhiteSpace(slot.entityId)) return;

            string state = slot.state.ToString();
            string progressKey = $"{slot.cellX}:{slot.cellY}:{slot.cellZ}:{slot.entityId}:{state}";
            _questService.ReportEvent(new QuestProgressEvent(
                QuestObjectiveType.StateReached,
                slot.entityId,
                state,
                1,
                progressKey));
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
        }
    }
}
#endif
