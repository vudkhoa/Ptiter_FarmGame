#if UNITY_EDITOR
using Core.Module.Cutscene;
using MyOwn.Bootstrap;
using MyOwn.ServiceHarness;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Module.Cutscene.Editor
{
    /// <summary>
    /// Dựng UI cutscene trong scene đang mở rồi tự kéo 3 field Cutscene của GameLifetimeScope.
    /// Chạy lại bao nhiêu lần cũng được: cái nào có sẵn thì tái dùng, thiếu thì tạo bù.
    /// </summary>
    public static class CutsceneSceneSetup
    {
        private const string CanvasName = "CutsceneCanvas";
        private const string ViewName = "CutsceneView";
        private const string InputName = "Input";
        private const string SkipButtonName = "SkipButton";
        private const string AutoPlayerName = "CutsceneAutoPlayer";
        private const string TemplateName = "ImageTemplate";
        private const string FontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // UIManager canvas đang ở sortingOrder 0 -> cutscene phải nằm trên toàn bộ HUD.
        private const int SortingOrder = 100;
        private const int UiLayer = 5;

        private static readonly Vector2 ReferenceResolution = new Vector2(2400, 1080);

        // Thứ tự PHẢI khớp enum CutsceneImageSlot vì slot index = index mảng _slots.
        private static readonly string[] SlotNames = { "Background", "Foreground", "Overlay" };

        [MenuItem("Tools/Cutscene/Setup Cutscene UI In Scene")]
        private static void Setup()
        {
            GameLifetimeScope scope =
                Object.FindAnyObjectByType<GameLifetimeScope>(FindObjectsInactive.Include);
            if (scope == null)
            {
                Debug.LogError(
                    "[CutsceneSetup] Scene đang mở không có GameLifetimeScope - mở MapScene rồi chạy lại.");
                return;
            }

            Transform parent = ResolveGameplayUiParent();
            if (parent == null)
            {
                Debug.LogWarning(
                    "[CutsceneSetup] Không tìm được GameplayRoot của MapSceneBootstrap - dựng UI ở gốc scene. " +
                    "CutsceneAutoPlayer sẽ chạy Start() trước khi container build, nhớ kéo lại vào GameplayRoot.");
            }

            GameObject canvas = EnsureCanvas(parent);
            CutsceneView view = EnsureView(canvas.transform);
            CutsceneSkipInput skipInput = EnsureSkipInput(view.transform);
            CutsceneAutoPlayer autoPlayer = EnsureAutoPlayer(parent);

            SetLayerRecursively(canvas, UiLayer);
            // Panel tắt sẵn; ShowAsync() bật lên, HideAsync() tắt lại.
            view.gameObject.SetActive(false);

            SerializedObject serialized = new SerializedObject(scope);
            serialized.FindProperty("_cutsceneView").objectReferenceValue = view;
            serialized.FindProperty("_cutsceneSkipInput").objectReferenceValue = skipInput;
            serialized.FindProperty("_cutsceneAutoPlayer").objectReferenceValue = autoPlayer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);

            EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
            Debug.Log(
                $"[CutsceneSetup] Đã dựng '{CanvasName}' và kéo đủ 3 field vào GameLifetimeScope. Nhớ Ctrl+S scene.",
                scope);
        }

        /// <summary>GameplayRoot đang tắt tới khi container build xong, nên UI cutscene phải nằm dưới đó.</summary>
        private static Transform ResolveGameplayUiParent()
        {
            MapSceneBootstrap bootstrap =
                Object.FindAnyObjectByType<MapSceneBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null) return null;

            GameObject root = new SerializedObject(bootstrap)
                .FindProperty("_gameplayRoot").objectReferenceValue as GameObject;
            if (root == null) return null;

            Transform ui = root.transform.Find("UI");
            return ui != null ? ui : root.transform;
        }

        private static GameObject EnsureCanvas(Transform parent)
        {
            Transform existing = FindChild(parent, CanvasName);
            if (existing != null) return existing.gameObject;

            GameObject go = new GameObject(
                CanvasName,
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Cutscene Canvas");

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static CutsceneView EnsureView(Transform canvas)
        {
            Transform existing = canvas.Find(ViewName);
            if (existing != null)
            {
                CutsceneView found = existing.GetComponent<CutsceneView>();
                if (found != null) return found;
            }

            GameObject go = new GameObject(
                ViewName, typeof(RectTransform), typeof(CanvasGroup), typeof(CutsceneView));
            Stretch(go, canvas);

            RectTransform[] slotParents = new RectTransform[SlotNames.Length];
            Image[] templates = new Image[SlotNames.Length];
            for (int i = 0; i < SlotNames.Length; i++)
            {
                GameObject slot = new GameObject(SlotNames[i], typeof(RectTransform));
                Stretch(slot, go.transform);

                GameObject template = new GameObject(
                    TemplateName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Stretch(template, slot.transform);

                Image image = template.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;              // ảnh không được nuốt tap chuyển khung
                image.color = new Color(1f, 1f, 1f, 0f);  // ShowImageTask fade alpha từ 0 lên
                template.SetActive(false);                // template chỉ làm gốc cho Instantiate

                slotParents[i] = slot.GetComponent<RectTransform>();
                templates[i] = image;
            }

            CutsceneView view = go.GetComponent<CutsceneView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_root").objectReferenceValue = go.GetComponent<CanvasGroup>();

            SerializedProperty slots = serialized.FindProperty("_slots");
            slots.arraySize = SlotNames.Length;
            for (int i = 0; i < SlotNames.Length; i++)
            {
                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("parent").objectReferenceValue = slotParents[i];
                element.FindPropertyRelative("template").objectReferenceValue = templates[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static CutsceneSkipInput EnsureSkipInput(Transform view)
        {
            Transform existing = view.Find(InputName);
            if (existing != null)
            {
                CutsceneSkipInput found = existing.GetComponent<CutsceneSkipInput>();
                if (found != null) return found;
            }

            // Vùng tap phủ toàn màn hình: Image trong suốt vẫn nhận raycast.
            GameObject go = new GameObject(
                InputName,
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CutsceneSkipInput));
            Stretch(go, view);
            go.GetComponent<Image>().color = Color.clear;

            Button skip = BuildSkipButton(go.transform);
            CutsceneSkipInput input = go.GetComponent<CutsceneSkipInput>();
            SerializedObject serialized = new SerializedObject(input);
            serialized.FindProperty("_skipButton").objectReferenceValue = skip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return input;
        }

        private static Button BuildSkipButton(Transform parent)
        {
            GameObject go = new GameObject(
                SkipButtonName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(220f, 90f);
            rect.anchoredPosition = new Vector2(-60f, -50f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            go.GetComponent<Button>().targetGraphic = image;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Stretch(labelGo, go.transform);
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "SKIP";
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            label.fontSize = 36f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static CutsceneAutoPlayer EnsureAutoPlayer(Transform parent)
        {
            Transform existing = FindChild(parent, AutoPlayerName);
            if (existing != null)
            {
                CutsceneAutoPlayer found = existing.GetComponent<CutsceneAutoPlayer>();
                if (found != null) return found;
            }

            GameObject go = new GameObject(AutoPlayerName, typeof(CutsceneAutoPlayer));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Cutscene Auto Player");

            CutsceneAutoPlayer player = go.GetComponent<CutsceneAutoPlayer>();
            string id = FindFirstCutsceneId();
            if (!string.IsNullOrWhiteSpace(id))
            {
                SerializedObject serialized = new SerializedObject(player);
                serialized.FindProperty("_cutsceneId").stringValue = id;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return player;
        }

        /// <summary>Điền sẵn id của CutsceneSO đầu tiên trong project cho đỡ phải gõ tay.</summary>
        private static string FindFirstCutsceneId()
        {
            string[] guids = AssetDatabase.FindAssets("t:CutsceneSO");
            for (int i = 0; i < guids.Length; i++)
            {
                CutsceneSO cutscene = AssetDatabase.LoadAssetAtPath<CutsceneSO>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (cutscene != null && !string.IsNullOrWhiteSpace(cutscene.cutsceneId))
                    return cutscene.cutsceneId;
            }
            return null;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent != null) return parent.Find(name);

            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name) return roots[i].transform;
            }
            return null;
        }

        private static void Stretch(GameObject go, Transform parent)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
#endif
