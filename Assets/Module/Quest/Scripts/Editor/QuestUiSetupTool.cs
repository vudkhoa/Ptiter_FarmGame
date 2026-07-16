#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Module.Quest.Editor
{
    public static class QuestUiSetupTool
    {
        private const string MenuPath = "Tools/Quest/Create Static Quest UI";

        [MenuItem(MenuPath)]
        public static void CreateStaticQuestUi()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                canvas = CreateCanvas();

            EnsureEventSystem();

            GameObject quest = CreateQuestPanel(canvas.transform);
            Undo.RegisterCreatedObjectUndo(quest, "Create Static Quest UI");
            Selection.activeGameObject = quest;
            EditorGUIUtility.PingObject(quest);
            EditorSceneManager.MarkSceneDirty(quest.scene);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static GameObject CreateQuestPanel(Transform parent)
        {
            GameObject quest = CreateUiObject(GetUniqueChildName(parent, "Quest"), parent);
            RectTransform questRect = quest.GetComponent<RectTransform>();
            questRect.anchorMin = new Vector2(1f, 1f);
            questRect.anchorMax = new Vector2(1f, 1f);
            questRect.pivot = new Vector2(1f, 1f);
            questRect.anchoredPosition = new Vector2(-32f, -32f);
            questRect.sizeDelta = new Vector2(360f, 420f);

            Image background = quest.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.11f, 0.86f);

            VerticalLayoutGroup layout = quest.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 18);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = quest.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(
                "Header",
                quest.transform,
                "QUESTS",
                24,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.88f, 0.35f));

            GameObject list = CreateUiObject("ObjectiveList", quest.transform);
            VerticalLayoutGroup listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            ContentSizeFitter listFitter = list.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateQuestItem(list.transform, "QuestItemTemplate", true);
            CreateQuestItem(list.transform, "QuestItemExample", false);

            return quest;
        }

        private static void CreateQuestItem(Transform parent, string name, bool template)
        {
            GameObject item = CreateUiObject(name, parent);
            Image background = item.AddComponent<Image>();
            background.color = template
                ? new Color(0.2f, 0.2f, 0.2f, 0.35f)
                : new Color(0.13f, 0.16f, 0.19f, 0.82f);

            VerticalLayoutGroup layout = item.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = item.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(
                "TitleText",
                item.transform,
                template ? "Quest title" : "Water the first plot",
                18,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                Color.white);

            CreateText(
                "DescriptionText",
                item.transform,
                template ? "Quest description" : "Reach the watered state on a farm slot.",
                14,
                FontStyles.Normal,
                TextAlignmentOptions.Left,
                new Color(0.82f, 0.86f, 0.9f));

            CreateText(
                "ProgressText",
                item.transform,
                template ? "0/1" : "watered: 0/1",
                14,
                FontStyles.Normal,
                TextAlignmentOptions.Left,
                new Color(0.65f, 1f, 0.68f));

            if (template)
                item.SetActive(false);
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;

            LayoutElement layout = textObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 6f;
            layout.flexibleWidth = 1f;

            return label;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            return gameObject;
        }

        private static string GetUniqueChildName(Transform parent, string desiredName)
        {
            bool exists = parent.Find(desiredName) != null;
            return exists ? GameObjectUtility.GetUniqueNameForSibling(parent, desiredName) : desiredName;
        }
    }
}
#endif
