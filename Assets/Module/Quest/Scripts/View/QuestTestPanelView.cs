#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Quest.View
{
    /// <summary>
    /// Debug MVC view. It listens to quest events and renders current model state directly;
    /// it does not own or copy quest data.
    /// </summary>
    public sealed class QuestTestPanelView : MonoBehaviour
    {
        private const float PanelWidth = 330f;
        private const float ScreenMargin = 16f;

        private QuestCatalogSO _catalog;
        private IQuestService _questService;
        private IDisposable _subscriptions;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _questNameStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _completedStyle;

        [Inject]
        public void Construct(
            QuestCatalogSO catalog,
            IQuestService questService,
            ISubscriber<QuestAcceptedPayload> accepted,
            ISubscriber<QuestProgressChangedPayload> progressChanged,
            ISubscriber<QuestCompletedPayload> completed)
        {
            _catalog = catalog;
            _questService = questService;

            var subscriptions = DisposableBag.CreateBuilder();
            accepted.Subscribe(_ => Repaint()).AddTo(subscriptions);
            progressChanged.Subscribe(_ => Repaint()).AddTo(subscriptions);
            completed.Subscribe(_ => Repaint()).AddTo(subscriptions);
            _subscriptions = subscriptions.Build();
        }

        private void Repaint()
        {
            // OnGUI reads the current model on the next Unity GUI pass.
            enabled = false;
            enabled = true;
        }

        private void OnGUI()
        {
            if (_catalog == null || _questService == null) return;
            EnsureStyles();

            int questCount = _catalog.quests != null ? _catalog.quests.Count : 0;
            float panelHeight = Mathf.Max(90f, 54f + questCount * 94f);
            var panelRect = new Rect(
                Screen.width - PanelWidth - ScreenMargin,
                ScreenMargin,
                PanelWidth,
                panelHeight);

            GUILayout.BeginArea(panelRect, _panelStyle);
            GUILayout.Label("QUESTS", _titleStyle);

            if (questCount == 0)
            {
                GUILayout.Label("No quests available", _bodyStyle);
            }
            else
            {
                for (int i = 0; i < questCount; i++)
                    DrawQuest(_catalog.quests[i], i < questCount - 1);
            }

            GUILayout.EndArea();
        }

        private void DrawQuest(QuestDefinitionSO definition, bool addSpacing)
        {
            if (definition == null) return;

            var state = _questService.GetQuestState(definition.questId);
            bool completed = state != null && state.Status == QuestStatus.Completed;
            GUILayout.Label(
                completed ? $"✓ {definition.questName}" : $"• {definition.questName}",
                completed ? _completedStyle : _questNameStyle);
            GUILayout.Label(definition.description, _bodyStyle);
            GUILayout.Label(BuildProgress(definition, state), completed ? _completedStyle : _bodyStyle);

            if (addSpacing) GUILayout.Space(8f);
        }

        private static string BuildProgress(QuestDefinitionSO definition, QuestRuntimeState state)
        {
            if (state == null) return "Not accepted";

            var text = new StringBuilder();
            for (int i = 0; i < definition.objectives.Count; i++)
            {
                var objective = definition.objectives[i];
                if (objective == null) continue;
                if (text.Length > 0) text.Append("  |  ");

                int current = 0;
                if (state.TryGetProgress(objective.objectiveId, out var progress))
                    current = progress.currentAmount;

                text.Append(objective.targetState)
                    .Append(": ")
                    .Append(current)
                    .Append('/')
                    .Append(Math.Max(1, objective.requiredAmount));
            }

            return text.ToString();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 12, 12),
                normal = { background = Texture2D.grayTexture }
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _questNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.88f, 0.35f) }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            _completedStyle = new GUIStyle(_bodyStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 1f, 0.5f) }
            };
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
    }
}
#endif
