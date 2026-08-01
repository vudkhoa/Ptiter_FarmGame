using System;
using System.Collections.Generic;
using Core.Module.Farm;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Core.Module.Quest
{
    /// <summary>
    /// Production adapter from the Farm module's public payloads into generic quest events.
    /// It deliberately does not modify or depend on Farm internals.
    /// </summary>
    public sealed class FarmQuestEventBridge : IStartable, IDisposable
    {
        private readonly IQuestService _questService;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly HashSet<string> _keysSeenThisFrame = new HashSet<string>();
        private readonly string _bridgeSessionId = Guid.NewGuid().ToString("N");
        private int _cachedFrame = -1;

        public FarmQuestEventBridge(
            IQuestService questService,
            ISubscriber<FarmEntityPlantedPayload> plantedSubscriber,
            ISubscriber<FarmEntityCaredPayload> caredSubscriber,
            ISubscriber<FarmEntityHarvestedPayload> harvestedSubscriber)
        {
            _questService = questService;
            _subscriptions.Add(plantedSubscriber.Subscribe(OnPlanted));
            _subscriptions.Add(caredSubscriber.Subscribe(OnCared));
            _subscriptions.Add(harvestedSubscriber.Subscribe(OnHarvested));
        }

        public void Start()
        {
            // Subscriptions are established by construction.
        }

        private void OnPlanted(FarmEntityPlantedPayload payload)
        {
            ReportAction(
                QuestEventType.FarmPlanted,
                payload.EntityId,
                payload.EntityType,
                payload.Cell);
        }

        private void OnCared(FarmEntityCaredPayload payload)
        {
            ReportAction(
                QuestEventType.FarmCared,
                payload.EntityId,
                payload.EntityType,
                payload.Cell);
        }

        private void OnHarvested(FarmEntityHarvestedPayload payload)
        {
            ReportAction(
                QuestEventType.FarmHarvestAction,
                payload.EntityId,
                payload.EntityType,
                payload.Cell);

            if (payload.Outputs == null) return;
            var amountByItem = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < payload.Outputs.Length; i++)
            {
                OutputReward output = payload.Outputs[i];
                if (output.item == null || output.amount <= 0) continue;
                string itemId = output.item.ItemId;
                if (string.IsNullOrWhiteSpace(itemId)) continue;
                amountByItem.TryGetValue(itemId, out int current);
                amountByItem[itemId] = current + output.amount;
            }

            foreach (KeyValuePair<string, int> output in amountByItem)
            {
                string key = BuildKey(
                    QuestEventType.FarmHarvestItem,
                    payload.EntityId,
                    payload.Cell,
                    output.Key);
                if (!TryUseKey(key)) continue;
                _questService.ReportEvent(new QuestProgressEvent(
                    QuestEventType.FarmHarvestItem,
                    output.Key,
                    "Item",
                    null,
                    output.Value,
                    key));
            }
        }

        private void ReportAction(
            QuestEventType eventType,
            string entityId,
            FarmEntityType entityType,
            Vector3Int cell)
        {
            if (string.IsNullOrWhiteSpace(entityId)) return;
            string key = BuildKey(eventType, entityId, cell, null);
            if (!TryUseKey(key)) return;
            _questService.ReportEvent(new QuestProgressEvent(
                eventType,
                entityId,
                entityType.ToString(),
                null,
                1,
                key));
        }

        private string BuildKey(
            QuestEventType eventType,
            string entityId,
            Vector3Int cell,
            string itemId)
        {
            return $"{_bridgeSessionId}:{UnityEngine.Time.frameCount}:{eventType}:" +
                   $"{entityId}:{cell.x},{cell.y},{cell.z}:{itemId}";
        }

        private bool TryUseKey(string key)
        {
            int frame = UnityEngine.Time.frameCount;
            if (_cachedFrame != frame)
            {
                _cachedFrame = frame;
                _keysSeenThisFrame.Clear();
            }
            return _keysSeenThisFrame.Add(key);
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
        }
    }
}
