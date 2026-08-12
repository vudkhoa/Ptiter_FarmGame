#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Core.Module.Tutorial.Integration.Farm;
using MyOwn.ServiceHarness;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Module.Tutorial.Editor
{
    internal sealed class TutorialAutoSetup : AssetPostprocessor
    {
        private const string SessionKey = "Tutorial.AutoSetupAttempted";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (!didDomainReload || SessionState.GetBool(SessionKey, false))
                return;
            if (AssetDatabase.LoadAssetAtPath<TutorialCatalogSO>(TutorialProjectSetup.CatalogPath) != null)
                return;

            // Same reason as StorageUIAutoSetup: a headless editor host never pumps delayCall,
            // so generation has to happen at the post-import boundary too.
            SessionState.SetBool(SessionKey, true);
            TutorialProjectSetup.Rebuild();
        }
    }

    /// Generates the tutorial assets: sprites, step/flow/catalog, the persistent UI container in
    /// the Preloading scene, and the build-button anchor. Re-runnable; overwrites only its output.
    public static class TutorialProjectSetup
    {
        private const string ModuleRoot = "Assets/Module/Tutorial";
        private const string ConfigRoot = ModuleRoot + "/Configs";
        private const string StepRoot = ConfigRoot + "/Steps";
        private const string TextureRoot = ModuleRoot + "/Texture";

        internal const string CatalogPath = ConfigRoot + "/TutorialCatalog.asset";
        private const string FirstFarmFlowPath = ConfigRoot + "/Flow_FirstFarm.asset";
        private const string FirstHarvestFlowPath = ConfigRoot + "/Flow_FirstHarvest.asset";

        private const string PreloadingScenePath = "Assets/myOwn/Scenes/Preloading.unity";
        private const string BuildHudButtonPrefabPath =
            "Assets/Module/UI System/Prefabs/HUD/BuildingHudButton.prefab";
        private const string FontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Itim SDF.asset";
        private const string FallbackFontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const string ContainerName = "[Tutorial UI Container]";
        private const string HandViewName = "Hand View";

        /// Matches the 400x326 source aspect so the sprite is never stretched (no preserveAspect).
        private static readonly Vector2 HandSize = new Vector2(184f, 150f);

        /// Fingertip position inside the hand sprite, measured from its alpha. This pivot is what
        /// lands a step offset of (0,0) on the anchor - re-measure it if the hand art is replaced.
        private static readonly Vector2 HandFingertipPivot = new Vector2(0.087f, 0.980f);

        /// Canvas-space lift for the cell ring and the hand on the place-land step: the grid anchor
        /// resolves slightly below the tile artwork it marks.
        private static readonly Vector2 CellAnchorCorrection = new Vector2(0f, 75f);

        // Above the UIManager canvas (0) and the Settings HUD (100). A focused modal outranks even
        // this, so TutorialOverlayCanvasRegistry lifts both canvases over it while one is open.
        private const int TutorialCanvasSortingOrder = 199;
        private const int ForegroundSortingOrder = 201;

        #region Public API
        [MenuItem("Tools/Tutorial/Rebuild Tutorial Content")]
        public static void Rebuild()
        {
            EnsureFolders();
            TutorialSprites sprites = EnsureSprites();
            TMP_FontAsset font = LoadFont();

            TutorialCatalogSO catalog = BuildCatalog(sprites);
            TutorialUIContainer container = BuildContainerInPreloadingScene(sprites, font, catalog);
            AddBuildButtonAnchor();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (container == null)
            {
                Debug.LogError("[TutorialSetup] Rebuild finished without a container - the hand cannot show.");
            }
        }

        [MenuItem("Tools/Tutorial/Reset Saved Tutorial Progress")]
        public static void ResetSavedProgress()
        {
            string path = Path.Combine(Application.persistentDataPath, "playerdata.json");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "Tutorial", "No save file - the tutorial already starts from scratch.", "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Tutorial",
                $"Delete this file to replay the tutorial:\n{path}\n\n" +
                "Or call ITutorialService.ResetProgress() in play mode to keep the rest of the save.",
                "Reveal");
            EditorUtility.RevealInFinder(path);
        }
        #endregion

        #region Private Methods
        [InitializeOnLoadMethod]
        private static void ScheduleEnsure() => EditorApplication.delayCall += EnsureSetup;

        private static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<TutorialCatalogSO>(CatalogPath) != null) return;

            Rebuild();
        }

        #region Folders
        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Module", "Tutorial");
            EnsureFolder(ModuleRoot, "Configs");
            EnsureFolder(ConfigRoot, "Steps");
            EnsureFolder(ModuleRoot, "Texture");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
        #endregion

        #region Sprites
        private struct TutorialSprites
        {
            public Sprite Hand;
            public Sprite FocusRing;
            public Sprite HintBubble;
            public Sprite CellDiamond;
        }

        private static TutorialSprites EnsureSprites()
        {
            return new TutorialSprites
            {
                Hand = EnsureSprite("tutorial_hand.png", 128, TouchIndicatorAlpha, 0),
                FocusRing = EnsureSprite(
                    "tutorial_focus_ring.png", 96,
                    (x, y, size) => RoundedOutlineAlpha(x, y, size, 28f, 6f),
                    32),
                HintBubble = EnsureSprite(
                    "tutorial_hint_bubble.png", 64,
                    (x, y, size) => RoundedFillAlpha(x, y, size, 22f),
                    24),
                // Isometric cell marker. Not 9-sliced: a diamond has to stretch as one shape,
                // slicing it would keep the corners square while the middle grew.
                CellDiamond = EnsureSprite(
                    "tutorial_cell_diamond.png", 256,
                    (x, y, size) => DiamondOutlineAlpha(x, y, size, 0.10f),
                    0),
            };
        }

        private static Sprite EnsureSprite(
            string fileName, int size, Func<int, int, int, float> alphaAt, int border)
        {
            string path = TextureRoot + "/" + fileName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alphaAt(x, y, size));
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
                if (border > 0)
                    importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// Filled dot with a detached outer ring - reads as "tap here" at any size.
        private static float TouchIndicatorAlpha(int x, int y, int size)
        {
            float half = size * 0.5f;
            float distance = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude;

            float core = Mathf.Clamp01(half * 0.42f - distance);
            float ringDistance = Mathf.Abs(distance - half * 0.78f);
            float ring = Mathf.Clamp01(half * 0.09f - ringDistance);

            return Mathf.Max(core, ring);
        }

        /// Hollow diamond: |dx| + |dy| == 1 in normalised space, banded by thickness.
        private static float DiamondOutlineAlpha(int x, int y, int size, float thickness)
        {
            float half = size * 0.5f;
            float dx = Mathf.Abs(x + 0.5f - half) / half;
            float dy = Mathf.Abs(y + 0.5f - half) / half;
            float edge = dx + dy;

            float outline = Mathf.Clamp01((thickness - Mathf.Abs(edge - 1f)) * (half * 0.5f));
            // Faint wash inside so the cell reads as selected, not just outlined.
            float fill = edge < 1f ? 0.22f : 0f;
            return Mathf.Max(outline, fill);
        }

        private static float RoundedFillAlpha(int x, int y, int size, float radius)
        {
            return Mathf.Clamp01(0.5f - RoundedRectDistance(x, y, size, radius));
        }

        private static float RoundedOutlineAlpha(int x, int y, int size, float radius, float thickness)
        {
            float distance = RoundedRectDistance(x, y, size, radius);
            float band = Mathf.Abs(distance + thickness * 0.5f) - thickness * 0.5f;
            return Mathf.Clamp01(0.5f - band);
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

        #region Configs
        private static TutorialCatalogSO BuildCatalog(TutorialSprites sprites)
        {
            // The build menu starts closed, so the very first beat is the HUD button that opens it.
            // Without this the flow used to point straight at a row inside a panel nobody had opened.
            // No hint text: the dim plus the hand on the one lit button already says "press this",
            // and the bubble covered the button on short screens. Empty hides the bubble entirely.
            TutorialStepSO openBuildMenu = BuildStep(
                "Step_OpenBuildMenu", "farm_open_build_menu",
                string.Empty,
                hand =>
                {
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.BuildButton;
                    hand.offset = Vector2.zero;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Tap;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = false;
                    step.dimBackground = true;
                    step.blockInputOutsideFocus = true;
                    step.completionSignal = TutorialSignal.BuildMenuOpened;
                });

            TutorialStepSO openLandUi = BuildStep(
                "Step_OpenLandUI", "farm_open_land_ui",
                "Chạm vào nút ô đất để bắt đầu nông trại nhé!",
                hand =>
                {
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.SoilButton;
                    hand.offset = Vector2.zero;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Tap;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = false;
                    step.dimBackground = true;
                    step.blockInputOutsideFocus = true;
                    step.completionSignal = TutorialSignal.LandPlacementStarted;
                });

            TutorialStepSO placeLand = BuildStep(
                "Step_PlaceLand", "farm_place_land",
                "Chạm vào khu đất trống để đặt ô đất xuống.",
                hand =>
                {
                    // A real buildable cell found by the bridge, not a fixed screen position:
                    // the middle of the screen is usually the shop roof, where placement refuses.
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.FreePlot;
                    // Matches focusOffset below: the cell anchor resolves a little under the tile
                    // artwork, so ring and fingertip are both lifted by the same amount.
                    hand.offset = CellAnchorCorrection;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Press;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = true;
                    step.focusSprite = sprites.CellDiamond;
                    // Roughly one isometric cell: twice as wide as tall.
                    step.focusFallbackSize = new Vector2(230f, 130f);
                    step.focusPadding = Vector2.zero;
                    step.focusOffset = CellAnchorCorrection;
                    step.dimBackground = true;
                    // Free placement: the map raycast must stay reachable, so the dim only darkens.
                    step.blockInputOutsideFocus = false;
                    step.hiddenAnchorIds = new List<string> { TutorialAnchorIds.ObjectsPanel };
                    step.completionSignal = TutorialSignal.LandPlaced;
                });

            TutorialStepSO plantSeed = BuildStep(
                "Step_PlantSeed", "farm_plant_seed",
                "Chạm vào ô đất để bắt đầu trồng cây.",
                hand =>
                {
                    // The plot the player just put down, not a fixed world point. The bridge re-pins
                    // this anchor from the save, so quitting after placing still leaves a target.
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.NewPlot;
                    hand.offset = Vector2.zero;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Tap;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = false;
                    step.focusFallbackSize = new Vector2(210f, 210f);
                    // The seed picker opens on top of this step; masking it would trap the player.
                    step.dimBackground = false;
                    step.blockInputOutsideFocus = false;
                    // Hands over to Step_ChooseSeed the moment the picker is up. Waiting for
                    // SeedPlanted here left the hand pointing at a plot hidden behind that picker.
                    step.completionSignal = TutorialSignal.SeedSelectorOpened;
                });

            TutorialStepSO chooseSeed = BuildStep(
                "Step_ChooseSeed", "farm_choose_seed",
                "Chọn một hạt giống để gieo xuống ô đất.",
                hand =>
                {
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.SeedItem;
                    hand.offset = Vector2.zero;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Tap;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = false;
                    step.focusFallbackSize = new Vector2(200f, 200f);
                    step.dimBackground = true;
                    step.blockInputOutsideFocus = true;
                    step.completionSignal = TutorialSignal.SeedPlanted;
                });

            TutorialStepSO harvest = BuildStep(
                "Step_HarvestCrop", "harvest_first_crop",
                "Cây đã chín rồi! Chạm vào cây để thu hoạch.",
                hand =>
                {
                    hand.anchorMode = TutorialAnchorMode.Anchor;
                    hand.anchorId = TutorialAnchorIds.RipeCrop;
                    hand.offset = Vector2.zero;
                    hand.rotation = 0f;
                    hand.animation = TutorialHandAnimation.Tap;
                },
                step =>
                {
                    step.hintOffset = new Vector2(0f, -90f);
                    step.showFocusRing = false;
                    step.focusFallbackSize = new Vector2(210f, 210f);
                    step.dimBackground = false;
                    step.blockInputOutsideFocus = false;
                    step.completionSignal = TutorialSignal.CropHarvested;
                });

            TutorialFlowSO firstFarm = BuildFlow(
                FirstFarmFlowPath, "first_farm", TutorialSignal.None,
                new[] { openBuildMenu, openLandUi, placeLand, plantSeed, chooseSeed });

            TutorialFlowSO firstHarvest = BuildFlow(
                FirstHarvestFlowPath, "first_harvest", TutorialSignal.CropRipe,
                new[] { harvest });

            TutorialCatalogSO catalog = LoadOrCreate<TutorialCatalogSO>(CatalogPath);
            catalog.tutorialEnabled = true;
            catalog.skipForExistingSaves = true;
            catalog.flows = new List<TutorialFlowSO> { firstFarm, firstHarvest };
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static TutorialStepSO BuildStep(
            string assetName,
            string stepId,
            string hint,
            Action<TutorialHandConfig> configureHand,
            Action<TutorialStepSO> configureStep)
        {
            TutorialStepSO step = LoadOrCreate<TutorialStepSO>($"{StepRoot}/{assetName}.asset");
            step.stepId = stepId;
            step.hintText = hint;
            step.hand = new TutorialHandConfig();
            step.startDelay = 0.35f;
            configureHand(step.hand);
            configureStep(step);
            EditorUtility.SetDirty(step);
            return step;
        }

        private static TutorialFlowSO BuildFlow(
            string path, string flowId, TutorialSignal startSignal, TutorialStepSO[] steps)
        {
            TutorialFlowSO flow = LoadOrCreate<TutorialFlowSO>(path);
            flow.flowId = flowId;
            flow.startSignal = startSignal;
            flow.steps = new List<TutorialStepSO>(steps);
            EditorUtility.SetDirty(flow);
            return flow;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        #endregion

        #region Preloading scene
        private static TutorialUIContainer BuildContainerInPreloadingScene(
            TutorialSprites sprites, TMP_FontAsset font, TutorialCatalogSO catalog)
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
                    $"[TutorialSetup] No RootLifetimeScope in {PreloadingScenePath}; " +
                    "the tutorial container has nowhere persistent to live.");
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
                return null;
            }

            // Parented to the root scope so it rides its DontDestroyOnLoad instead of adding a
            // second persistent root, and so RegisterComponentInHierarchy stays unnecessary.
            Transform existingContainer = FindDeep(root.transform, ContainerName);
            if (existingContainer != null)
                UnityEngine.Object.DestroyImmediate(existingContainer.gameObject);

            TutorialUIContainer container = CreateContainer(root.transform, sprites, font);

            SerializedObject rootSo = new SerializedObject(root);
            Set(rootSo, "_tutorialCatalog", catalog);
            Set(rootSo, "_tutorialUIContainer", container);
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            return container;
        }

        private static TutorialUIContainer CreateContainer(
            Transform parent, TutorialSprites sprites, TMP_FontAsset font)
        {
            GameObject containerObject = new GameObject(
                ContainerName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TutorialUIContainer));
            containerObject.transform.SetParent(parent, false);

            Canvas canvas = containerObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = TutorialCanvasSortingOrder;

            CanvasScaler scaler = containerObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject handViewObject = new GameObject(
                HandViewName, typeof(RectTransform), typeof(TutorialHandView));
            RectTransform handViewRect = Stretch(handViewObject, containerObject.transform);

            // Sibling order is the depth order: dim at the back, cloned widget over it, hand and
            // ring on top. TutorialDimMask, not a plain Image, so a gated map cell can report a miss.
            GameObject dimmerObject = new GameObject(
                "Dimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TutorialDimMask));
            Stretch(dimmerObject, handViewRect);

            TutorialDimMask dimmer = dimmerObject.GetComponent<TutorialDimMask>();
            dimmer.color = new Color(0f, 0f, 0f, 0.62f);
            dimmer.raycastTarget = true;
            dimmerObject.SetActive(false);

            // Starts inactive and stays that way until a step clones into it. That is deliberate:
            // cloning into an inactive parent keeps the copy's own scripts from ever waking up.
            GameObject highlightObject = new GameObject("Highlight", typeof(RectTransform));
            RectTransform highlightRoot = Centered(
                highlightObject, handViewRect, Vector2.zero, Vector2.zero);
            highlightObject.SetActive(false);

            GameObject foregroundObject = new GameObject(
                "Foreground", typeof(RectTransform), typeof(Canvas));
            RectTransform foreground = Stretch(foregroundObject, handViewRect);
            Canvas foregroundCanvas = foregroundObject.GetComponent<Canvas>();
            foregroundCanvas.overrideSorting = true;
            foregroundCanvas.sortingOrder = ForegroundSortingOrder;

            Image focusRing = ImageObject(
                "Focus Ring", foreground, sprites.FocusRing,
                Vector2.zero, new Vector2(220f, 220f));
            focusRing.type = Image.Type.Sliced;
            focusRing.color = new Color(1f, 0.93f, 0.55f, 0.95f);
            focusRing.raycastTarget = false;

            GameObject handRootObject = new GameObject("Hand Root", typeof(RectTransform));
            RectTransform handRoot = Centered(handRootObject, foreground, Vector2.zero, Vector2.one);

            Image hand = ImageObject("Hand", handRoot, sprites.Hand, Vector2.zero, HandSize);
            hand.raycastTarget = false;
            hand.rectTransform.pivot = HandFingertipPivot;
            // Re-apply: changing pivot after anchoredPosition shifts the rect.
            hand.rectTransform.anchoredPosition = Vector2.zero;
            CanvasGroup handGroup = hand.gameObject.AddComponent<CanvasGroup>();
            handGroup.blocksRaycasts = false;
            handGroup.interactable = false;

            Image hintBackground = ImageObject(
                "Hint Root", foreground, sprites.HintBubble,
                new Vector2(0f, 180f), new Vector2(720f, 130f));
            hintBackground.type = Image.Type.Sliced;
            hintBackground.color = new Color(0.15f, 0.10f, 0.07f, 0.86f);
            hintBackground.raycastTarget = false;

            TextMeshProUGUI hintLabel = Text(
                "Hint Label", hintBackground.rectTransform, string.Empty,
                Vector2.zero, new Vector2(660f, 110f), 38f, font);
            hintLabel.color = Color.white;
            hintLabel.raycastTarget = false;

            TutorialHandView view = handViewObject.GetComponent<TutorialHandView>();
            SerializedObject viewSo = new SerializedObject(view);
            Set(viewSo, "_canvas", canvas);
            Set(viewSo, "_canvasRect", handViewRect);
            Set(viewSo, "_handRoot", handRoot);
            Set(viewSo, "_hand", hand.rectTransform);
            Set(viewSo, "_handGroup", handGroup);
            Set(viewSo, "_focusRing", focusRing.rectTransform);
            Set(viewSo, "_focusImage", focusRing);
            Set(viewSo, "_dimmer", dimmer);
            Set(viewSo, "_highlightRoot", highlightRoot);
            Set(viewSo, "_hintRoot", hintBackground.rectTransform);
            Set(viewSo, "_hintLabel", hintLabel);
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            TutorialUIContainer container = containerObject.GetComponent<TutorialUIContainer>();
            SerializedObject containerSo = new SerializedObject(container);
            Set(containerSo, "_handView", view);
            containerSo.FindProperty("_persistAcrossScenes").boolValue = false;
            containerSo.ApplyModifiedPropertiesWithoutUndo();

            handViewObject.SetActive(false);
            return container;
        }
        #endregion

        #region Build button anchor
        /// Only the HUD button is a fixed widget. The soil row and the panel root are runtime
        /// instances, so they register from code (ObjectSelectSlotView / SelectObjectsScreen).
        private static void AddBuildButtonAnchor()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildHudButtonPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[TutorialSetup] {BuildHudButtonPrefabPath} is missing - " +
                    "the first tutorial step has no button to point at.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(BuildHudButtonPrefabPath);
            try
            {
                EnsureAnchor(root, TutorialAnchorIds.BuildButton);
                PrefabUtility.SaveAsPrefabAsset(root, BuildHudButtonPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureAnchor(GameObject host, string anchorId)
        {
            TutorialAnchor anchor = host.GetComponent<TutorialAnchor>();
            if (anchor == null) anchor = host.AddComponent<TutorialAnchor>();

            SerializedObject anchorSo = new SerializedObject(anchor);
            anchorSo.FindProperty("_anchorId").stringValue = anchorId;
            anchorSo.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion

        #region UI helpers
        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font != null) return font;

            Debug.LogWarning($"[TutorialSetup] {FontAssetPath} is missing - falling back to LiberationSans.");
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

        private static RectTransform Centered(
            GameObject target, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image ImageObject(
            string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject target = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Centered(target, parent, position, size);

            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static TextMeshProUGUI Text(
            string name, Transform parent, string value,
            Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Centered(target, parent, position, size);

            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
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
