#if UNITY_EDITOR
using System;
using System.IO;
using MyOwn.ServiceHarness;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Module.Toast.Editor
{
    internal sealed class ToastAutoSetup : AssetPostprocessor
    {
        private const string SessionKey = "Toast.AutoSetupAttempted";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (!didDomainReload || SessionState.GetBool(SessionKey, false))
                return;
            if (!ToastProjectSetup.NeedsRebuild) return;

            // Same reason as the tutorial setup: a headless editor host never pumps delayCall,
            // so generation has to happen at the post-import boundary too.
            SessionState.SetBool(SessionKey, true);
            ToastProjectSetup.Rebuild();
        }
    }

    /// Generates the toast assets: the bubble sprite, the config, and the persistent UI container
    /// in the Preloading scene wired onto RootLifetimeScope. Re-runnable; overwrites only its output.
    public static class ToastProjectSetup
    {
        /// Bumped whenever the generated container's shape changes. The ToastItem prefab is NEVER
        /// regenerated - it is authored by hand after the first run.
        private const int SetupVersion = 2;

        private const string ModuleRoot = "Assets/Module/Toast";
        private const string ConfigRoot = ModuleRoot + "/Configs";
        private const string TextureRoot = ModuleRoot + "/Texture";
        private const string PrefabRoot = ModuleRoot + "/Prefabs";

        internal const string ConfigPath = ConfigRoot + "/ToastConfig.asset";
        internal const string ItemPrefabPath = PrefabRoot + "/ToastItem.prefab";

        private const string PreloadingScenePath = "Assets/myOwn/Scenes/Preloading.unity";
        private const string FontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Itim SDF.asset";
        private const string FallbackFontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const string ContainerName = "[Toast UI Container]";
        private const string StackName = "Stack";
        private const string ItemPrefabName = "ToastItem";

        /// Matches the tutorial container so a toast is sized against the same design space.
        private static readonly Vector2 ReferenceResolution = new Vector2(2400f, 1080f);

        #region Properties
        internal static bool NeedsRebuild
        {
            get
            {
                ToastConfigSO config = AssetDatabase.LoadAssetAtPath<ToastConfigSO>(ConfigPath);
                return config == null ||
                       config.setupVersion != SetupVersion ||
                       AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath) == null;
            }
        }
        #endregion

        #region Public API
        [MenuItem("Tools/Toast/Rebuild Toast Content")]
        public static void Rebuild()
        {
            EnsureFolders();
            Sprite bubble = EnsureBubbleSprite();
            TMP_FontAsset font = LoadFont();
            ToastConfigSO config = EnsureConfig();
            ToastItemView itemPrefab = EnsureItemPrefab(bubble, font);

            ToastUIContainer container = BuildContainerInPreloadingScene(itemPrefab, config);

            config.setupVersion = SetupVersion;
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (container == null)
            {
                Debug.LogError("[ToastSetup] Rebuild finished without a container - toasts stay off.");
            }
        }
        #endregion

        #region Private Methods
        [InitializeOnLoadMethod]
        private static void ScheduleEnsure() => EditorApplication.delayCall += EnsureSetup;

        private static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!NeedsRebuild) return;

            Rebuild();
        }

        #region Folders
        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Module", "Toast");
            EnsureFolder(ModuleRoot, "Configs");
            EnsureFolder(ModuleRoot, "Texture");
            EnsureFolder(ModuleRoot, "Prefabs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
        #endregion

        #region Assets
        private static ToastConfigSO EnsureConfig()
        {
            ToastConfigSO config = AssetDatabase.LoadAssetAtPath<ToastConfigSO>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<ToastConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        /// 9-sliced pill. Generated rather than authored so the module ships self-contained.
        private static Sprite EnsureBubbleSprite()
        {
            const int Size = 64;
            const int Border = 24;
            string path = TextureRoot + "/toast_bubble.png";

            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, RoundedFillAlpha(x, y, Size, 22f));
            }
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = new Vector4(Border, Border, Border, Border);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float RoundedFillAlpha(int x, int y, int size, float radius)
        {
            return Mathf.Clamp01(0.5f - RoundedRectDistance(x, y, size, radius));
        }

        /// Signed distance to a rounded rectangle filling the texture. Negative inside.
        private static float RoundedRectDistance(int x, int y, int size, float radius)
        {
            float half = size * 0.5f;
            float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
            float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float outsideX = Mathf.Max(px, 0f);
            float outsideY = Mathf.Max(py, 0f);
            return new Vector2(outsideX, outsideY).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
        }
        #endregion

        #region Preloading scene
        private static ToastUIContainer BuildContainerInPreloadingScene(
            ToastItemView itemPrefab, ToastConfigSO config)
        {
            Scene existing = SceneManager.GetSceneByPath(PreloadingScenePath);
            bool wasLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = wasLoaded
                ? existing
                : EditorSceneManager.OpenScene(PreloadingScenePath, OpenSceneMode.Additive);

            RootLifetimeScope root = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                root = rootObject.GetComponentInChildren<RootLifetimeScope>(true);
                if (root != null) break;
            }

            if (root == null)
            {
                Debug.LogError(
                    $"[ToastSetup] No RootLifetimeScope in {PreloadingScenePath}; " +
                    "the toast container has nowhere persistent to live.");
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
                return null;
            }

            // Parented to the root scope so it rides its DontDestroyOnLoad instead of adding a
            // second persistent root.
            Transform previous = FindDeep(root.transform, ContainerName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            ToastUIContainer container = CreateContainer(root.transform, itemPrefab, config);

            SerializedObject rootSo = new SerializedObject(root);
            Set(rootSo, "_toastUIContainer", container);
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            return container;
        }

        private static ToastUIContainer CreateContainer(
            Transform parent, ToastItemView itemPrefab, ToastConfigSO config)
        {
            GameObject containerObject = new GameObject(
                ContainerName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(ToastUIContainer));
            containerObject.transform.SetParent(parent, false);

            // Deliberately NO GraphicRaycaster: a toast is a message, never a target. Adding one
            // would let the topmost canvas in the game swallow the tap that raised it.
            Canvas canvas = containerObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ToastCanvasRegistry.ToastSortingOrder;

            CanvasScaler scaler = containerObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject stackObject = new GameObject(StackName, typeof(RectTransform));
            RectTransform stack = Stretch(stackObject, containerObject.transform);

            ToastUIContainer container = containerObject.GetComponent<ToastUIContainer>();
            SerializedObject containerSo = new SerializedObject(container);
            Set(containerSo, "_config", config);
            Set(containerSo, "_canvas", canvas);
            Set(containerSo, "_stack", stack);
            Set(containerSo, "_itemPrefab", itemPrefab);
            containerSo.FindProperty("_persistAcrossScenes").boolValue = false;
            containerSo.ApplyModifiedPropertiesWithoutUndo();

            return container;
        }
        #endregion

        #region Item prefab
        /// Created once and then left alone: this is the asset the designer restyles. A rebuild
        /// only regenerates the container that points at it.
        private static ToastItemView EnsureItemPrefab(Sprite bubble, TMP_FontAsset font)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath);
            if (existing != null) return existing.GetComponent<ToastItemView>();

            GameObject itemObject = BuildItemObject(bubble, font);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(itemObject, ItemPrefabPath);
            UnityEngine.Object.DestroyImmediate(itemObject);

            return saved != null ? saved.GetComponent<ToastItemView>() : null;
        }

        private static GameObject BuildItemObject(Sprite bubble, TMP_FontAsset font)
        {
            GameObject itemObject = new GameObject(
                ItemPrefabName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(ToastItemView));

            RectTransform rect = itemObject.GetComponent<RectTransform>();
            // Anchored to the middle of the screen with a centred pivot: anchoredPosition.y is then
            // a pure offset from the centre line, which is what the container stacks along.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(700f, 140f);

            Image background = itemObject.GetComponent<Image>();
            background.sprite = bubble;
            background.type = Image.Type.Sliced;
            // Alpha here is only what the prefab looks like in the editor; at runtime
            // ToastConfigSO.backgroundOpacity drives it unless overrideBackgroundColor is off.
            background.color = new Color(0.09f, 0.07f, 0.05f, 0.78f);
            background.raycastTarget = false;

            HorizontalLayoutGroup layout = itemObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 26, 26);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = itemObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(itemObject.transform, false);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Toast";
            label.font = font;
            label.fontSize = 42f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = Color.white;
            label.raycastTarget = false;

            CanvasGroup group = itemObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            ToastItemView view = itemObject.GetComponent<ToastItemView>();
            SerializedObject viewSo = new SerializedObject(view);
            Set(viewSo, "_rect", rect);
            Set(viewSo, "_group", group);
            Set(viewSo, "_background", background);
            Set(viewSo, "_label", label);
            Set(viewSo, "_fitter", fitter);
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // Saved active: the container disables each clone itself, and a prefab that opens
            // greyed-out in the editor is the kind of thing a designer files a bug about.
            return itemObject;
        }
        #endregion

        #region Helpers
        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font != null) return font;

            Debug.LogWarning($"[ToastSetup] {FontAssetPath} is missing - falling back to LiberationSans.");
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);
        }

        private static RectTransform Stretch(GameObject target, Transform parent)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void Set(SerializedObject target, string name, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Serialized field '{name}' was not found.");
            property.objectReferenceValue = value;
        }
        #endregion
        #endregion
    }
}
#endif
