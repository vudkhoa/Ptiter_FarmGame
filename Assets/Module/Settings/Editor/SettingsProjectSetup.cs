#if UNITY_EDITOR
using System;
using BrunoMikoski.UIManager;
using Core.Module.Settings.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Settings.Editor
{
    public static class SettingsProjectSetup
    {
        private const string WindowCollectionPath =
            "Assets/Module/UI System/package/SO/Windows/UIWindowCollection.asset";
        private const string SettingsWindowPrefabPath =
            "Assets/Module/Settings/UI/SettingsWindow.prefab";
        private const string PopupLayerPath =
            "Assets/Module/UI System/package/SO/Layers/Items/Popup.asset";
        private const string ItimFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Itim SDF.asset";
        private const string TextureRoot =
            "Assets/Module/Settings/Texture/";

        private static readonly Color TextBrown =
            new Color32(139, 75, 34, 255);
        private static readonly Color DimColor =
            new Color(0.09f, 0.07f, 0.05f, 0.55f);

        [MenuItem("Tools/Settings/Rebuild Settings UI")]
        public static void RebuildSettingsUI()
        {
            UIWindowCollection collection =
                AssetDatabase.LoadAssetAtPath<UIWindowCollection>(
                    WindowCollectionPath);
            if (collection == null)
            {
                Debug.LogError(
                    $"[SettingsSetup] UIWindowCollection is missing at " +
                    $"{WindowCollectionPath}.");
                return;
            }

            PrefabUIWindow window =
                collection.GetOrAddNew<PrefabUIWindow>("SettingsWindow");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    SettingsWindowPrefabPath) != null)
                AssetDatabase.DeleteAsset(SettingsWindowPrefabPath);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                ItimFontPath);
            ConfigureTextureImports();
            Sprite panelSprite = LoadSprite("Rectangle 160.png");
            Sprite headerSprite = LoadSprite("Rectangle 1242.png");
            Sprite contentSprite = LoadSprite("Rectangle 156.png");
            Sprite toggleSprite = LoadSprite("On Off Button.png");
            Sprite closeSprite = LoadSprite("Union.png");
            Sprite musicSprite = LoadSprite("Icon_Setting.png");
            Sprite soundSprite = LoadSprite("Icon_Setting (1).png");
            Sprite vibrationSprite = LoadSprite("Icon_Setting (2).png");
            Sprite dividerSprite = LoadSprite("Line 7.png");

            if (panelSprite == null || headerSprite == null ||
                contentSprite == null || toggleSprite == null ||
                closeSprite == null || musicSprite == null ||
                soundSprite == null || vibrationSprite == null ||
                dividerSprite == null)
            {
                Debug.LogError(
                    "[SettingsSetup] One or more Settings textures are " +
                    $"missing from {TextureRoot}.");
                return;
            }

            GameObject root = new GameObject(
                "SettingsWindow",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(WindowControllerEvents),
                typeof(SettingsWindowController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Canvas>().overrideSorting = true;

            Button overlayButton = OverlayButton(root.transform);
            overlayButton.transition = Selectable.Transition.None;

            ImageObject(
                "Panel", root.transform, Vector2.zero,
                new Vector2(972f, 807f), panelSprite, false);
            ImageObject(
                "Header", root.transform, new Vector2(0f, 321.5f),
                new Vector2(972f, 164f), headerSprite, false);
            ImageObject(
                "Content", root.transform, new Vector2(0f, -74f),
                new Vector2(902f, 561f), contentSprite, false);

            TextMeshProUGUI title = TextObject(
                "Title", root.transform, "CÀI ĐẶT",
                new Vector2(0f, 322f), new Vector2(560f, 110f),
                64f, font, Color.white, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.outlineColor = TextBrown;
            title.outlineWidth = 0.22f;

            Button closeButton = ImageButton(
                "Close Button", root.transform,
                new Vector2(400f, 323f), new Vector2(80f, 80f),
                closeSprite);

            ImageObject(
                "Divider 1", root.transform, new Vector2(0f, 12f),
                new Vector2(763f, 4f), dividerSprite, false);
            ImageObject(
                "Divider 2", root.transform, new Vector2(0f, -154f),
                new Vector2(763f, 4f), dividerSprite, false);

            Toggle musicToggle = ToggleRow(
                root.transform, "Music", "NHẠC NỀN", 95f,
                musicSprite, toggleSprite, font,
                out RectTransform musicVisual, out Image musicImage);
            Toggle soundToggle = ToggleRow(
                root.transform, "Sound", "ÂM THANH", -71f,
                soundSprite, toggleSprite, font,
                out RectTransform soundVisual, out Image soundImage);
            Toggle vibrationToggle = ToggleRow(
                root.transform, "Vibration", "RUNG", -238f,
                vibrationSprite, toggleSprite, font,
                out RectTransform vibrationVisual,
                out Image vibrationImage);

            SettingsWindowController controller =
                root.GetComponent<SettingsWindowController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "_closeButton", closeButton);
            Set(serialized, "_musicToggle", musicToggle);
            Set(serialized, "_soundToggle", soundToggle);
            Set(serialized, "_vibrationToggle", vibrationToggle);
            Set(serialized, "_musicToggleVisual", musicVisual);
            Set(serialized, "_soundToggleVisual", soundVisual);
            Set(serialized, "_vibrationToggleVisual", vibrationVisual);
            Set(serialized, "_musicToggleImage", musicImage);
            Set(serialized, "_soundToggleImage", soundImage);
            Set(serialized, "_vibrationToggleImage", vibrationImage);
            SetWindowReference(serialized, window, collection);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                root, SettingsWindowPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (prefab == null)
            {
                Debug.LogError(
                    "[SettingsSetup] Failed to save SettingsWindow prefab.");
                return;
            }

            SerializedObject windowSerialized = new SerializedObject(window);
            windowSerialized.FindProperty("windowControllerPrefab")
                .objectReferenceValue =
                prefab.GetComponent<SettingsWindowController>();
            UILayer popup = AssetDatabase.LoadAssetAtPath<UILayer>(
                PopupLayerPath);
            if (popup != null)
                windowSerialized.FindProperty("layer").objectReferenceValue =
                    popup;
            windowSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(window);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[SettingsSetup] Settings UI rebuilt from final mock assets.");
        }

        private static Button OverlayButton(Transform parent)
        {
            GameObject gameObject = new GameObject(
                "Overlay Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            Image image = gameObject.GetComponent<Image>();
            image.color = DimColor;
            image.raycastTarget = true;
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Toggle ToggleRow(
            Transform parent,
            string name,
            string labelText,
            float y,
            Sprite iconSprite,
            Sprite toggleSprite,
            TMP_FontAsset font,
            out RectTransform visualRect,
            out Image visualImage)
        {
            GameObject row = RectObject(
                $"{name} Row", parent,
                new Vector2(0f, y), new Vector2(820f, 120f));
            ImageObject(
                "Icon", row.transform, new Vector2(-340f, 0f),
                new Vector2(60f, 60f), iconSprite, false);

            TextMeshProUGUI label = TextObject(
                "Label", row.transform, labelText,
                new Vector2(-95f, 0f), new Vector2(410f, 90f),
                48f, font, TextBrown, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;

            GameObject toggleObject = new GameObject(
                "Toggle",
                typeof(RectTransform),
                typeof(Toggle));
            RectTransform toggleRect =
                (RectTransform)toggleObject.transform;
            toggleRect.SetParent(row.transform, false);
            toggleRect.anchorMin = toggleRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(320f, 0f);
            toggleRect.sizeDelta = new Vector2(152f, 76f);

            visualImage = ImageObject(
                "Visual", toggleObject.transform, Vector2.zero,
                new Vector2(152f, 76f), toggleSprite, true);
            visualRect = visualImage.rectTransform;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = visualImage;
            toggle.graphic = null;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = true;
            return toggle;
        }

        private static Button ImageButton(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Sprite sprite)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private static Image ImageObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            bool raycastTarget)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TextMeshProUGUI TextObject(
            string name,
            Transform parent,
            string text,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if (font != null) label.font = font;
            return label;
        }

        private static GameObject RectObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return gameObject;
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(TextureRoot + fileName);
        }

        private static void ConfigureTextureImports()
        {
            string[] files =
            {
                "Rectangle 160.png",
                "Rectangle 1242.png",
                "Rectangle 156.png",
                "On Off Button.png",
                "Union.png",
                "Icon_Setting.png",
                "Icon_Setting (1).png",
                "Icon_Setting (2).png",
                "Line 7.png"
            };

            for (int i = 0; i < files.Length; i++)
            {
                string path = TextureRoot + files[i];
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled ||
                               !importer.alphaIsTransparency;
                if (!changed) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        private static void Set(
            SerializedObject target,
            string name,
            UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException(
                    $"Serialized field '{name}' was not found on " +
                    $"{target.targetObject.GetType().Name}.");
            property.objectReferenceValue = value;
        }

        private static void SetWindowReference(
            SerializedObject controller,
            PrefabUIWindow window,
            UIWindowCollection collection)
        {
            SerializedProperty controllerWindow =
                controller.FindProperty("window");
            SerializedObject windowAsset = new SerializedObject(window);
            SerializedProperty itemGuid = windowAsset.FindProperty("guid");
            SerializedProperty collectionGuid =
                windowAsset.FindProperty("collectionGUID");

            if (controllerWindow == null || itemGuid == null ||
                collectionGuid == null)
                throw new InvalidOperationException(
                    "UIManager GUID fields could not be resolved.");

            CopyLong(itemGuid, "value1", controllerWindow,
                "collectionItemGUIDValueA");
            CopyLong(itemGuid, "value2", controllerWindow,
                "collectionItemGUIDValueB");
            CopyLong(collectionGuid, "value1", controllerWindow,
                "collectionGUIDValueA");
            CopyLong(collectionGuid, "value2", controllerWindow,
                "collectionGUIDValueB");
            controllerWindow.FindPropertyRelative("itemLastKnownName")
                .stringValue = window.name;
            controllerWindow.FindPropertyRelative("collectionLastKnownName")
                .stringValue = collection.name;
        }

        private static void CopyLong(
            SerializedProperty source,
            string sourceName,
            SerializedProperty destination,
            string destinationName)
        {
            destination.FindPropertyRelative(destinationName).longValue =
                source.FindPropertyRelative(sourceName).longValue;
        }
    }
}
#endif
