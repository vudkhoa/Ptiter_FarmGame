#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
using Core.Module.Quest;
using MyOwn.ServiceHarness;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest.Editor
{
    public static class QuestProjectSetup
    {
        private const string ConfigRoot = "Assets/Module/Quest/Configs/Daily";
        private const string UiRoot = "Assets/Module/Quest/UI";
        private const string TextureRoot = "Assets/Module/Quest/Texture";
        private const string QuestWindowPrefabPath =
            UiRoot + "/QuestWindow.prefab";
        private const string WindowCollectionPath =
            "Assets/Module/UI System/package/SO/Windows/UIWindowCollection.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticSetup()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        [MenuItem("Tools/Quest/Rebuild Quest Content & UI")]
        public static void Rebuild()
        {
            EnsureFolders();
            ImportQuestTexturesAsSprites();
            BuildDailyContent();
            BuildQuestWindowPrefab(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestSetup] Daily content and Quest UI were rebuilt.");
        }

        private static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            QuestCatalogSO catalog = AssetDatabase.LoadAssetAtPath<QuestCatalogSO>(
                "Assets/Module/Quest/Configs/QuestCatalog.asset");
            bool contentReady =
                AssetDatabase.LoadAssetAtPath<DailyQuestScheduleSO>(
                    ConfigRoot + "/DailyQuestSchedule.asset") != null &&
                catalog != null &&
                catalog.dailySchedule != null;
            bool uiReady =
                AssetDatabase.LoadAssetAtPath<GameObject>(QuestWindowPrefabPath) != null &&
                AssetDatabase.LoadAssetAtPath<PrefabUIWindow>(
                    "Assets/Module/UI System/package/SO/Windows/Items/QuestWindow.asset") != null;
            if (contentReady && uiReady) return;

            EnsureFolders();
            ImportQuestTexturesAsSprites();
            BuildDailyContent();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(QuestWindowPrefabPath) == null)
                BuildQuestWindowPrefab(false);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Module/Quest", "Configs");
            EnsureFolder("Assets/Module/Quest/Configs", "Daily");
            EnsureFolder("Assets/Module/Quest", "UI");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void ImportQuestTexturesAsSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                if (importer.textureType == TextureImporterType.Sprite &&
                    !importer.mipmapEnabled &&
                    importer.alphaIsTransparency)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void BuildDailyContent()
        {
            QuestCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<QuestCatalogSO>(
                    "Assets/Module/Quest/Configs/QuestCatalog.asset");
            if (catalog == null) return;

            Sprite plantIcon = Sprite("quest hàng ngày_ hộp quà 1.png");
            Sprite careIcon = Sprite("quest cho ăn 1.png");
            Sprite harvestIcon = Sprite("quest thu hoạch 1.png");

            var definitions = new List<QuestDefinitionSO>
            {
                Definition("daily_s1_plant_wheat", "Gieo lúa mì",
                    "Gieo 3 cây lúa mì.", plantIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmPlanted,
                    QuestTargetScope.ExactTarget, "c_wheat", null, 3),
                Definition("daily_s1_care_animals", "Chăm sóc vật nuôi",
                    "Cho vật nuôi ăn 3 lần.", careIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmCared,
                    QuestTargetScope.TargetCategory, null, "Animal", 3),
                Definition("daily_s1_harvest", "Thu hoạch",
                    "Thực hiện 2 lần thu hoạch.", harvestIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmHarvestAction,
                    QuestTargetScope.Any, null, null, 2),
                Definition("daily_s1_wheat_amount", "Thu thập lúa mì",
                    "Thu hoạch tổng cộng 10 lúa mì.", harvestIcon,
                    QuestObjectiveType.ItemAmount, QuestEventType.FarmHarvestItem,
                    QuestTargetScope.ExactTarget, "wheat_grain", null, 10),

                Definition("daily_s2_plant_sugarcane", "Gieo mía",
                    "Gieo 3 cây mía.", plantIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmPlanted,
                    QuestTargetScope.ExactTarget, "c_sugarcane", null, 3),
                Definition("daily_s2_care_crops", "Chăm sóc cây trồng",
                    "Chăm sóc cây trồng 3 lần.", careIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmCared,
                    QuestTargetScope.TargetCategory, null, "Crop", 3),
                Definition("daily_s2_harvest_animals", "Thu hoạch chuồng nuôi",
                    "Thu hoạch vật nuôi 2 lần.", harvestIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmHarvestAction,
                    QuestTargetScope.TargetCategory, null, "Animal", 2),
                Definition("daily_s2_eggs", "Thu thập trứng",
                    "Thu hoạch tổng cộng 5 quả trứng.", harvestIcon,
                    QuestObjectiveType.ItemAmount, QuestEventType.FarmHarvestItem,
                    QuestTargetScope.ExactTarget, "egg", null, 5),

                Definition("daily_s3_plant_crops", "Gieo cây trồng",
                    "Gieo bất kỳ 5 cây trồng.", plantIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmPlanted,
                    QuestTargetScope.TargetCategory, null, "Crop", 5),
                Definition("daily_s3_care_animals", "Cho vật nuôi ăn",
                    "Chăm sóc vật nuôi 4 lần.", careIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmCared,
                    QuestTargetScope.TargetCategory, null, "Animal", 4),
                Definition("daily_s3_harvest_sugarcane", "Thu hoạch mía",
                    "Thu hoạch mía 3 lần.", harvestIcon,
                    QuestObjectiveType.ActionCount, QuestEventType.FarmHarvestAction,
                    QuestTargetScope.ExactTarget, "c_sugarcane", null, 3),
                Definition("daily_s3_sugarcane_amount", "Thu thập mía",
                    "Thu hoạch tổng cộng 12 cây mía.", harvestIcon,
                    QuestObjectiveType.ItemAmount, QuestEventType.FarmHarvestItem,
                    QuestTargetScope.ExactTarget, "sugarcane_raw", null, 12)
            };

            DailyQuestSetSO set1 = Set("daily_set_1", definitions.GetRange(0, 4));
            DailyQuestSetSO set2 = Set("daily_set_2", definitions.GetRange(4, 4));
            DailyQuestSetSO set3 = Set("daily_set_3", definitions.GetRange(8, 4));
            DailyQuestScheduleSO schedule =
                LoadOrCreate<DailyQuestScheduleSO>(
                    ConfigRoot + "/DailyQuestSchedule.asset");
            schedule.utcOffsetHours = 7;
            schedule.sets = new List<DailyQuestSetSO> { set1, set2, set3 };
            EditorUtility.SetDirty(schedule);

            catalog.dailySchedule = schedule;
            catalog.quests ??= new List<QuestDefinitionSO>();
            for (int i = 0; i < definitions.Count; i++)
            {
                if (!catalog.quests.Contains(definitions[i]))
                    catalog.quests.Add(definitions[i]);
            }
            EditorUtility.SetDirty(catalog);
        }

        private static QuestDefinitionSO Definition(
            string id,
            string title,
            string description,
            Sprite icon,
            QuestObjectiveType objectiveType,
            QuestEventType eventType,
            QuestTargetScope targetScope,
            string targetId,
            string targetCategory,
            int required)
        {
            QuestDefinitionSO definition =
                LoadOrCreate<QuestDefinitionSO>($"{ConfigRoot}/{id}.asset");
            definition.questId = id;
            definition.category = QuestCategory.Daily;
            definition.questName = title;
            definition.description = description;
            definition.icon = icon;
            definition.objectives = new List<QuestObjectiveData>
            {
                new QuestObjectiveData
                {
                    objectiveId = id + "_objective",
                    objectiveType = objectiveType,
                    eventType = eventType,
                    targetScope = targetScope,
                    targetId = targetId,
                    targetCategory = targetCategory,
                    requiredAmount = required
                }
            };
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static DailyQuestSetSO Set(
            string id,
            List<QuestDefinitionSO> definitions)
        {
            DailyQuestSetSO set =
                LoadOrCreate<DailyQuestSetSO>($"{ConfigRoot}/{id}.asset");
            set.setId = id;
            set.quests = new List<DailyQuestEntry>();
            for (int i = 0; i < definitions.Count; i++)
            {
                set.quests.Add(new DailyQuestEntry
                {
                    quest = definitions[i],
                    points = 25,
                    coinReward = 100
                });
            }
            set.milestones = new List<DailyMilestoneDefinition>
            {
                new DailyMilestoneDefinition
                    { milestoneId = "points_25", requiredPoints = 25, coinReward = 100 },
                new DailyMilestoneDefinition
                    { milestoneId = "points_50", requiredPoints = 50, coinReward = 300 },
                new DailyMilestoneDefinition
                    { milestoneId = "points_100", requiredPoints = 100, coinReward = 500 }
            };
            EditorUtility.SetDirty(set);
            return set;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void BuildQuestWindowPrefab(bool overwrite)
        {
            if (overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(
                    QuestWindowPrefabPath) != null)
                AssetDatabase.DeleteAsset(QuestWindowPrefabPath);

            GameObject root = new GameObject(
                "QuestWindow",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(WindowControllerEvents),
                typeof(QuestWindowController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1800, 1200);
            root.GetComponent<Canvas>().overrideSorting = true;

            GameObject daily = ImageObject(
                "Daily Panel", root.transform,
                Sprite("quest hàng ngày_nền 2.png"),
                Vector2.zero, new Vector2(1800, 1200));
            GameObject progress = ImageObject(
                "Progress Placeholder", root.transform,
                Sprite("quest tiến độ 3.png"),
                Vector2.zero, new Vector2(1800, 1200));
            GameObject food = ImageObject(
                "Food Placeholder", root.transform,
                Sprite("quest thực đơn 3.png"),
                Vector2.zero, new Vector2(1800, 1200));
            progress.SetActive(false);
            food.SetActive(false);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            TextMeshProUGUI title = Text(
                "Title", root.transform, "NHIỆM VỤ",
                new Vector2(0, 505), new Vector2(500, 90), 48, font);
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            Button dailyTab = Button(
                "Daily Tab", root.transform, "Hằng ngày",
                new Vector2(-500, 330), new Vector2(420, 95), font);
            Button progressTab = Button(
                "Progress Tab", root.transform, "Tiến độ",
                new Vector2(0, 330), new Vector2(420, 95), font);
            Button foodTab = Button(
                "Food Tab", root.transform, "Thực đơn",
                new Vector2(500, 330), new Vector2(420, 95), font);
            Button close = Button(
                "Close", root.transform, "X",
                new Vector2(770, 500), new Vector2(70, 70), font);

            TextMeshProUGUI countdown = Text(
                "Countdown", daily.transform, "00:00:00",
                new Vector2(680, 245), new Vector2(250, 50), 28, font);
            TextMeshProUGUI points = Text(
                "Total Points", daily.transform, "0",
                new Vector2(0, 220), new Vector2(180, 50), 28, font);
            TextMeshProUGUI locked = Text(
                "Locked Reason", daily.transform, "Synchronizing server time...",
                new Vector2(0, 40), new Vector2(900, 70), 30, font);

            QuestTaskItemView[] taskSlots =
            {
                TaskSlot("Task 1", daily.transform, new Vector2(0, 30), font),
                TaskSlot("Task 2", daily.transform, new Vector2(0, -220), font)
            };
            QuestMilestoneView[] milestones =
            {
                Milestone("Milestone 1", daily.transform, new Vector2(-430, 225), font),
                Milestone("Milestone 2", daily.transform, new Vector2(0, 225), font),
                Milestone("Milestone 3", daily.transform, new Vector2(430, 225), font)
            };

            Button previous = Button(
                "Previous Page", daily.transform, "<",
                new Vector2(-160, -495), new Vector2(90, 60), font);
            TextMeshProUGUI page = Text(
                "Page", daily.transform, "1/2",
                new Vector2(0, -495), new Vector2(120, 60), 28, font);
            Button next = Button(
                "Next Page", daily.transform, ">",
                new Vector2(160, -495), new Vector2(90, 60), font);

            GameObject toast = ImageObject(
                "Reward Toast", root.transform, null,
                new Vector2(0, 0), new Vector2(300, 130));
            toast.GetComponent<Image>().color = new Color(0.1f, 0.45f, 0.15f, 0.95f);
            TextMeshProUGUI toastText = Text(
                "Reward Text", toast.transform, "+100",
                Vector2.zero, new Vector2(280, 100), 48, font);
            toast.SetActive(false);

            QuestWindowController controller = root.GetComponent<QuestWindowController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "_dailyTab", dailyTab);
            Set(serialized, "_progressTab", progressTab);
            Set(serialized, "_foodTab", foodTab);
            Set(serialized, "_closeButton", close);
            Set(serialized, "_previousPage", previous);
            Set(serialized, "_nextPage", next);
            Set(serialized, "_dailyPanel", daily);
            Set(serialized, "_progressPlaceholder", progress);
            Set(serialized, "_foodPlaceholder", food);
            Set(serialized, "_countdown", countdown);
            Set(serialized, "_pageLabel", page);
            Set(serialized, "_totalPoints", points);
            Set(serialized, "_lockedReason", locked);
            SetArray(serialized, "_taskSlots", taskSlots);
            SetArray(serialized, "_milestones", milestones);
            Set(serialized, "_rewardToast", toast);
            Set(serialized, "_rewardToastText", toastText);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                root, QuestWindowPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            UIWindowCollection collection =
                AssetDatabase.LoadAssetAtPath<UIWindowCollection>(
                    WindowCollectionPath);
            if (collection == null || prefab == null) return;
            PrefabUIWindow window =
                collection.GetOrAddNew<PrefabUIWindow>("QuestWindow");
            SerializedObject windowSerialized = new SerializedObject(window);
            windowSerialized.FindProperty("windowControllerPrefab").objectReferenceValue =
                prefab.GetComponent<QuestWindowController>();
            UILayer popup = AssetDatabase.LoadAssetAtPath<UILayer>(
                "Assets/Module/UI System/package/SO/Layers/Items/Popup.asset");
            if (popup != null)
                windowSerialized.FindProperty("layer").objectReferenceValue = popup;
            windowSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(window);
            EditorUtility.SetDirty(collection);
        }

        private static QuestTaskItemView TaskSlot(
            string name, Transform parent, Vector2 position, TMP_FontAsset font)
        {
            GameObject root = ImageObject(
                name, parent, null, position, new Vector2(1350, 215));
            root.GetComponent<Image>().color = new Color(1f, 0.87f, 0.58f, 0.88f);
            QuestTaskItemView view = root.AddComponent<QuestTaskItemView>();
            Image icon = ImageObject(
                "Icon", root.transform, Sprite("quest thu hoạch 1.png"),
                new Vector2(-570, 0), new Vector2(130, 120)).GetComponent<Image>();
            TextMeshProUGUI title = Text(
                "Title", root.transform, "Nhiệm vụ",
                new Vector2(-300, 50), new Vector2(430, 55), 30, font);
            title.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI description = Text(
                "Description", root.transform, "Mô tả",
                new Vector2(-250, 5), new Vector2(530, 45), 22, font);
            description.alignment = TextAlignmentOptions.Left;
            Slider slider = SliderObject(
                root.transform, new Vector2(-120, -60), new Vector2(700, 28));
            TextMeshProUGUI progress = Text(
                "Progress", root.transform, "0/1",
                new Vector2(280, -60), new Vector2(120, 45), 22, font);
            TextMeshProUGUI reward = Text(
                "Reward", root.transform, "100",
                new Vector2(555, 0), new Vector2(160, 70), 30, font);
            GameObject complete = Text(
                "Completed", root.transform, "✓",
                new Vector2(630, 70), new Vector2(60, 60), 38, font).gameObject;
            GameObject pending = Text(
                "Pending", root.transform, "...",
                new Vector2(630, -70), new Vector2(70, 50), 25, font).gameObject;

            SerializedObject serialized = new SerializedObject(view);
            Set(serialized, "_icon", icon);
            Set(serialized, "_title", title);
            Set(serialized, "_description", description);
            Set(serialized, "_progress", progress);
            Set(serialized, "_progressBar", slider);
            Set(serialized, "_reward", reward);
            Set(serialized, "_completedMark", complete);
            Set(serialized, "_pendingMark", pending);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static QuestMilestoneView Milestone(
            string name, Transform parent, Vector2 position, TMP_FontAsset font)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            Rect(root, parent, position, new Vector2(260, 100));
            QuestMilestoneView view = root.AddComponent<QuestMilestoneView>();
            Button claim = Button(
                "Claim", root.transform, "NHẬN",
                Vector2.zero, new Vector2(230, 90), font);
            TextMeshProUGUI points = Text(
                "Points", root.transform, "25",
                new Vector2(-75, 0), new Vector2(70, 45), 22, font);
            TextMeshProUGUI reward = Text(
                "Reward", root.transform, "100",
                new Vector2(65, 0), new Vector2(90, 45), 22, font);
            GameObject locked = Text(
                "Locked", root.transform, "🔒",
                new Vector2(0, -45), new Vector2(60, 40), 20, font).gameObject;
            GameObject claimed = Text(
                "Claimed", root.transform, "✓",
                new Vector2(0, -45), new Vector2(60, 40), 25, font).gameObject;
            GameObject pending = Text(
                "Pending", root.transform, "...",
                new Vector2(0, -45), new Vector2(70, 40), 22, font).gameObject;

            SerializedObject serialized = new SerializedObject(view);
            Set(serialized, "_points", points);
            Set(serialized, "_reward", reward);
            Set(serialized, "_claimButton", claim);
            Set(serialized, "_locked", locked);
            Set(serialized, "_claimed", claimed);
            Set(serialized, "_pending", pending);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Slider SliderObject(
            Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = ImageObject("Progress Bar", parent, null, position, size);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.18f, 0.42f, 0.12f, 0.45f);
            Slider slider = root.AddComponent<Slider>();
            GameObject fill = ImageObject(
                "Fill", root.transform, null, Vector2.zero, size);
            fill.GetComponent<Image>().color = new Color(0.1f, 0.55f, 0.12f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.interactable = false;
            return slider;
        }

        private static GameObject ImageObject(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Rect(gameObject, parent, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.color = sprite != null ? Color.white : new Color(1, 1, 1, 0.25f);
            return gameObject;
        }

        private static Button Button(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            TMP_FontAsset font)
        {
            GameObject gameObject = ImageObject(name, parent, null, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(0.84f, 0.39f, 0.17f, 0.95f);
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text(name + " Label", gameObject.transform, label, Vector2.zero, size, 28, font);
            return button;
        }

        private static TextMeshProUGUI Text(
            string name,
            Transform parent,
            string value,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Rect(gameObject, parent, position, size);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.34f, 0.17f, 0.12f);
            text.raycastTarget = false;
            return text;
        }

        private static void Rect(
            GameObject gameObject,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Set(
            SerializedObject target, string property, UnityEngine.Object value)
        {
            target.FindProperty(property).objectReferenceValue = value;
        }

        private static void SetArray<T>(
            SerializedObject target, string property, T[] values)
            where T : UnityEngine.Object
        {
            SerializedProperty array = target.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Sprite Sprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                TextureRoot + "/" + fileName);
        }
    }
}
#endif
