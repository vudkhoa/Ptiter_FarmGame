#if UNITY_EDITOR
using System;
using BrunoMikoski.UIManager;
using Core.Module.Storage.View;
using Shared.Utils;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Module.Storage.Editor
{
    internal sealed class StorageUIAutoSetup : AssetPostprocessor
    {
        private const string SessionKey = "StorageUI.AutoSetupAttempted";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (!didDomainReload || SessionState.GetBool(SessionKey, false))
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Module/Storage/UI/InventoryWindow.prefab") != null)
                return;

            // This project is also opened by a headless editor host that does not
            // pump EditorApplication.delayCall. Run once at the safe post-import
            // boundary so generated assets are still created in that environment.
            SessionState.SetBool(SessionKey, true);
            StorageUIProjectSetup.Rebuild();
        }
    }

    public static class StorageUIProjectSetup
    {
        private const string ModuleRoot = "Assets/Module/Storage";
        private const string UiRoot = ModuleRoot + "/UI";
        private const string ConfigRoot = ModuleRoot + "/Configs";
        private const string TextureRoot = ModuleRoot + "/Texture";
        private const string InventoryPrefabPath = UiRoot + "/InventoryWindow.prefab";
        private const string CatalogPath = ConfigRoot + "/InventoryCatalog.asset";
        private const string MapScenePath = "Assets/Module/Map/Scenes/MapScene.unity";
        private const string WindowCollectionPath =
            "Assets/Module/UI System/package/SO/Windows/UIWindowCollection.asset";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/Itim-Regular.ttf";
        private const string FontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Itim SDF.asset";

        [MenuItem("Tools/Storage/Rebuild Inventory UI")]
        public static void Rebuild()
        {
            EnsureFolders();
            ImportTexturesAsSprites();
            TMP_FontAsset font = EnsureItimFontAsset();
            InventoryCatalogSO catalog = BuildCatalog();
            PrefabUIWindow window = BuildInventoryWindow(catalog, font, true);
            PlaceWarehouseInMap(window);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StorageUI] Inventory prefab, catalog and map warehouse rebuilt.");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleEnsure()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        private static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath) != null &&
                AssetDatabase.LoadAssetAtPath<InventoryCatalogSO>(CatalogPath) != null &&
                AssetDatabase.LoadAssetAtPath<PrefabUIWindow>(
                    "Assets/Module/UI System/package/SO/Windows/Items/InventoryWindow.asset") != null)
                return;

            Rebuild();
        }

        private static void EnsureFolders()
        {
            EnsureFolder(ModuleRoot, "UI");
            EnsureFolder(ModuleRoot, "Configs");
            EnsureFolder("Assets/TextMesh Pro/Resources", "Fonts & Materials");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void ImportTexturesAsSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                if (importer.textureType == TextureImporterType.Sprite &&
                    !importer.mipmapEnabled && importer.alphaIsTransparency)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static TMP_FontAsset EnsureItimFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null)
            {
                Debug.LogError($"[StorageUI] Itim source font is missing at {FontPath}.");
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            font.name = "Itim SDF";
            AssetDatabase.CreateAsset(font, FontAssetPath);
            if (font.material != null)
            {
                font.material.name = "Itim SDF Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            if (font.atlasTextures != null)
            {
                for (int i = 0; i < font.atlasTextures.Length; i++)
                {
                    Texture2D atlas = font.atlasTextures[i];
                    if (atlas == null || AssetDatabase.Contains(atlas)) continue;
                    atlas.name = $"Itim SDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(atlas, font);
                }
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.ImportAsset(FontAssetPath);
            return font;
        }

        private static InventoryCatalogSO BuildCatalog()
        {
            InventoryCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<InventoryCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<InventoryCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty items = serialized.FindProperty("_items");
            items.arraySize = 1;
            SerializedProperty bonsai = items.GetArrayElementAtIndex(0);
            bonsai.FindPropertyRelative("itemId").stringValue = "bonsai";
            bonsai.FindPropertyRelative("displayName").stringValue = "CÂY BONSAI";
            bonsai.FindPropertyRelative("description").stringValue =
                "Mua ở tiệm cây cảnh";
            bonsai.FindPropertyRelative("category").enumValueIndex =
                (int)InventoryCategory.Decoration;
            bonsai.FindPropertyRelative("icon").objectReferenceValue =
                Sprite("UI/inventory_item_bonsai_icon.png");
            bonsai.FindPropertyRelative("preview").objectReferenceValue =
                Sprite("UI/inventory_item_bonsai_preview.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static PrefabUIWindow BuildInventoryWindow(
            InventoryCatalogSO catalog,
            TMP_FontAsset font,
            bool overwrite)
        {
            UIWindowCollection collection =
                AssetDatabase.LoadAssetAtPath<UIWindowCollection>(WindowCollectionPath);
            if (collection == null)
            {
                Debug.LogError("[StorageUI] UIWindowCollection is missing.");
                return null;
            }

            PrefabUIWindow window =
                collection.GetOrAddNew<PrefabUIWindow>("InventoryWindow");
            if (overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath) != null)
                AssetDatabase.DeleteAsset(InventoryPrefabPath);

            GameObject root = new GameObject(
                "InventoryWindow",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(WindowControllerEvents),
                typeof(InventoryWindowController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            // Match the final reference composition: the inventory occupies most
            // of the scene while retaining enough map visibility around it.
            rootRect.sizeDelta = new Vector2(2200, 1200);
            root.GetComponent<Canvas>().overrideSorting = true;

            Image overlay = ImageObject("Dimmer", root.transform, null,
                Vector2.zero, new Vector2(2400, 1400)).GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.28f);

            GameObject inventoryPanel = ImageObject(
                "Inventory Panel", root.transform, Sprite("UI/inventory_panel_left_bg.png"),
                new Vector2(-285, -25), new Vector2(900, 687));
            GameObject detailPanel = ImageObject(
                "Detail Panel", root.transform, Sprite("UI/inventory_panel_detail_bg.png"),
                new Vector2(535, -25), new Vector2(480, 800));

            ImageObject("Title Board", root.transform,
                Sprite("UI/inventory_title_banner.png"),
                new Vector2(-165, 420), new Vector2(700, 168));
            TextMeshProUGUI title = Text("Title", root.transform, "KHO LƯU TRỮ",
                new Vector2(-165, 425), new Vector2(640, 105), 58, font);
            title.color = Color.white;

            ImageObject("Title Hanger Left", root.transform,
                Sprite("UI/inventory_title_hanger.png"),
                new Vector2(-405, 345), new Vector2(38, 78));
            ImageObject("Title Hanger Right", root.transform,
                Sprite("UI/inventory_title_hanger.png"),
                new Vector2(75, 345), new Vector2(38, 78));

            Button allTab = Tab("All Tab", root.transform,
                Sprite("UI/inventory_tab_all_bg.png"),
                "Tất cả", new Vector2(-775, 145), font);
            Button farmTab = Tab("Farm Tab", root.transform,
                Sprite("UI/inventory_tab_farm_bg.png"),
                "Nông sản", new Vector2(-775, 25), font);
            Button decorationTab = Tab("Decoration Tab", root.transform,
                Sprite("UI/inventory_tab_decoration_bg.png"), "Trang trí",
                new Vector2(-775, -95), font);

            InventoryItemView[] slots = new InventoryItemView[12];
            for (int i = 0; i < slots.Length; i++)
            {
                int row = i / 4;
                int column = i % 4;
                slots[i] = ItemSlot(
                    $"Item Slot {i + 1}", inventoryPanel.transform,
                    new Vector2(-245 + column * 155, 180 - row * 155), font);
            }

            Button previous = ArtworkButton("Previous Page", inventoryPanel.transform,
                Sprite("UI/inventory_page_previous_icon.png"),
                new Vector2(-310, -245), new Vector2(46, 50));
            Button next = ArtworkButton("Next Page", inventoryPanel.transform,
                Sprite("UI/inventory_page_next_icon.png"),
                new Vector2(310, -245), new Vector2(46, 50));
            TextMeshProUGUI page = Text("Page", inventoryPanel.transform, "1 / 1",
                new Vector2(0, -245), new Vector2(140, 45), 25, font);
            Image pageBg = ImageObject("Page Background", inventoryPanel.transform,
                Sprite("UI/inventory_page_indicator_bg.png"), new Vector2(0, -245),
                new Vector2(100, 36)).GetComponent<Image>();
            pageBg.transform.SetAsFirstSibling();

            GameObject emptyDetails = new GameObject("Empty Details", typeof(RectTransform));
            Rect(emptyDetails, detailPanel.transform, Vector2.zero, new Vector2(440, 740));
            TextMeshProUGUI hint = Text("Hint", emptyDetails.transform,
                "Bấm vào vật phẩm bất kỳ\nđể xem chi tiết",
                Vector2.zero, new Vector2(410, 220), 32, font);
            hint.color = new Color(0.40f, 0.16f, 0.12f);

            GameObject selectedDetails = new GameObject("Selected Details", typeof(RectTransform));
            Rect(selectedDetails, detailPanel.transform, Vector2.zero, new Vector2(440, 760));
            Image preview = ImageObject("Preview", selectedDetails.transform,
                Sprite("UI/inventory_item_bonsai_preview.png"), new Vector2(0, 220),
                new Vector2(420, 300)).GetComponent<Image>();
            TextMeshProUGUI itemName = Text("Item Name", selectedDetails.transform,
                "CÂY BONSAI", new Vector2(0, -15), new Vector2(410, 65), 32, font);
            itemName.color = new Color(0.16f, 0.42f, 0.16f);
            TextMeshProUGUI description = Text("Description", selectedDetails.transform,
                "Mua ở tiệm cây cảnh", new Vector2(0, -90),
                new Vector2(410, 80), 27, font);
            description.color = new Color(0.40f, 0.16f, 0.12f);
            GameObject actionVisual = ImageObject("Future Action Button", selectedDetails.transform,
                Sprite("UI/inventory_action_button_bg.png"),
                new Vector2(0, -235), new Vector2(156, 65));
            actionVisual.GetComponent<Image>().raycastTarget = false;
            selectedDetails.SetActive(false);

            Button close = ArtworkButton(
                "Temporary Close", root.transform,
                Sprite("UI/inventory_close_button.png"),
                new Vector2(742, 275), new Vector2(92, 92));

            InventoryWindowController controller =
                root.GetComponent<InventoryWindowController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            Set(controllerSo, "_catalog", catalog);
            Set(controllerSo, "_allTab", allTab);
            Set(controllerSo, "_farmTab", farmTab);
            Set(controllerSo, "_decorationTab", decorationTab);
            Set(controllerSo, "_previousPage", previous);
            Set(controllerSo, "_nextPage", next);
            Set(controllerSo, "_closeButton", close);
            Set(controllerSo, "_pageLabel", page);
            SetArray(controllerSo, "_itemSlots", slots);
            Set(controllerSo, "_emptyDetails", emptyDetails);
            Set(controllerSo, "_selectedDetails", selectedDetails);
            Set(controllerSo, "_preview", preview);
            Set(controllerSo, "_itemName", itemName);
            Set(controllerSo, "_description", description);
            Set(controllerSo, "_actionButtonVisual", actionVisual);
            SetWindowReference(controllerSo, window, collection);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, InventoryPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null) return window;

            SerializedObject windowSo = new SerializedObject(window);
            windowSo.FindProperty("windowControllerPrefab").objectReferenceValue =
                prefab.GetComponent<InventoryWindowController>();
            UILayer popup = AssetDatabase.LoadAssetAtPath<UILayer>(
                "Assets/Module/UI System/package/SO/Layers/Items/Popup.asset");
            if (popup != null)
                windowSo.FindProperty("layer").objectReferenceValue = popup;
            windowSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(window);
            EditorUtility.SetDirty(collection);
            return window;
        }

        private static void PlaceWarehouseInMap(PrefabUIWindow window)
        {
            if (window == null) return;
            Scene existing = SceneManager.GetSceneByPath(MapScenePath);
            bool wasLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = wasLoaded
                ? existing
                : EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Additive);

            GameObject warehouse = null;
            Transform mapParent = null;
            WindowsManager manager = null;
            Camera mapCamera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, "Inventory Warehouse");
                if (found != null) warehouse = found.gameObject;
                mapParent ??= FindDeep(root.transform, "Map");
                manager ??= root.GetComponentInChildren<WindowsManager>(true);
                mapCamera ??= root.GetComponentInChildren<Camera>(true);
            }

            if (warehouse == null)
            {
                warehouse = new GameObject(
                    "Inventory Warehouse",
                    typeof(SpriteRenderer),
                    typeof(BoxCollider),
                    typeof(WindowOpenTrigger));
                if (mapParent != null) warehouse.transform.SetParent(mapParent, false);
            }

            warehouse.transform.position = new Vector3(35f, 1.75f, 35f);
            warehouse.transform.localScale = Vector3.one * 0.34f;
            if (mapCamera != null)
                warehouse.transform.rotation = mapCamera.transform.rotation;
            SpriteRenderer renderer = warehouse.GetComponent<SpriteRenderer>();
            renderer.sprite = Sprite("World/inventory_warehouse_interaction.png");
            renderer.sortingOrder = 50;
            BoxCollider collider = warehouse.GetComponent<BoxCollider>();
            if (renderer.sprite != null)
            {
                Vector2 spriteSize = renderer.sprite.bounds.size;
                // Match the interaction footprint to the full warehouse artwork.
                const float interactionScale = 1f;
                collider.size = new Vector3(
                    spriteSize.x * interactionScale,
                    spriteSize.y * interactionScale,
                    0.25f);
            }

            SerializedObject trigger =
                new SerializedObject(warehouse.GetComponent<WindowOpenTrigger>());
            Set(trigger, "_windowsManager", manager);
            Set(trigger, "_window", window);
            trigger.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        private static InventoryItemView ItemSlot(
            string name, Transform parent, Vector2 position, TMP_FontAsset font)
        {
            GameObject root = ImageObject(
                name, parent, Sprite("UI/inventory_slot_default_bg.png"),
                position, new Vector2(95, 118));
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            Image icon = ImageObject(
                "Icon", root.transform, Sprite("UI/inventory_item_bonsai_icon.png"),
                new Vector2(0, 10), new Vector2(72, 72)).GetComponent<Image>();
            TextMeshProUGUI amount = Text("Amount", root.transform, "6",
                new Vector2(0, -39), new Vector2(76, 32), 23, font);
            amount.color = new Color(0.30f, 0.16f, 0.08f);
            InventoryItemView view = root.AddComponent<InventoryItemView>();
            SerializedObject so = new SerializedObject(view);
            Set(so, "_button", button);
            Set(so, "_background", root.GetComponent<Image>());
            Set(so, "_icon", icon);
            Set(so, "_amount", amount);
            Set(so, "_normalSprite", Sprite("UI/inventory_slot_default_bg.png"));
            Set(so, "_selectedSprite", Sprite("UI/inventory_slot_selected_bg.png"));
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button Tab(
            string name, Transform parent, Sprite sprite, string label,
            Vector2 position, TMP_FontAsset font)
        {
            Button button = ArtworkButton(name, parent, sprite, position,
                new Vector2(190, 92));
            TextMeshProUGUI text = Text("Label", button.transform, label,
                Vector2.zero, new Vector2(160, 58), 24, font);
            text.color = new Color(0.38f, 0.15f, 0.08f);
            text.raycastTarget = false;
            return button;
        }

        private static Button ArtworkButton(
            string name, Transform parent, Sprite sprite,
            Vector2 position, Vector2 size)
        {
            GameObject root = ImageObject(name, parent, sprite, position, size);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            return button;
        }

        private static Button SimpleButton(
            string name, Transform parent, string label,
            Vector2 position, Vector2 size, TMP_FontAsset font)
        {
            GameObject root = ImageObject(name, parent, null, position, size);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.75f, 0.25f, 0.14f, 0.96f);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI text = Text("Label", root.transform, label,
                Vector2.zero, size, 34, font);
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static GameObject ImageObject(
            string name, Transform parent, Sprite sprite,
            Vector2 position, Vector2 size)
        {
            GameObject result = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Rect(result, parent, position, size);
            Image image = result.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            return result;
        }

        private static TextMeshProUGUI Text(
            string name, Transform parent, string value,
            Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            GameObject result = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Rect(result, parent, position, size);
            TextMeshProUGUI text = result.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Rect(
            GameObject target, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Sprite Sprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                TextureRoot + "/" + relativePath);
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

        private static void SetArray<T>(SerializedObject target, string name, T[] values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = target.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetWindowReference(
            SerializedObject controller,
            PrefabUIWindow window,
            UIWindowCollection collection)
        {
            SerializedProperty controllerWindow = controller.FindProperty("window");
            SerializedObject windowAsset = new SerializedObject(window);
            SerializedProperty itemGuid = windowAsset.FindProperty("guid");
            SerializedProperty collectionGuid = windowAsset.FindProperty("collectionGUID");
            CopyLong(itemGuid, "value1", controllerWindow, "collectionItemGUIDValueA");
            CopyLong(itemGuid, "value2", controllerWindow, "collectionItemGUIDValueB");
            CopyLong(collectionGuid, "value1", controllerWindow, "collectionGUIDValueA");
            CopyLong(collectionGuid, "value2", controllerWindow, "collectionGUIDValueB");
            controllerWindow.FindPropertyRelative("itemLastKnownName").stringValue = window.name;
            controllerWindow.FindPropertyRelative("collectionLastKnownName").stringValue =
                collection.name;
        }

        private static void CopyLong(
            SerializedProperty source, string sourceName,
            SerializedProperty destination, string destinationName)
        {
            destination.FindPropertyRelative(destinationName).longValue =
                source.FindPropertyRelative(sourceName).longValue;
        }
    }
}
#endif
