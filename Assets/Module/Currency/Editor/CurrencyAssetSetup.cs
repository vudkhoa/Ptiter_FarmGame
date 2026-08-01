#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Currency.Editor
{
    [InitializeOnLoad]
    public static class CurrencyAssetSetup
    {
        private const string ResourceRoot =
            "Assets/Module/Currency/Resources/Currency";
        private const string FrameSource =
            "Assets/Module/Quest/Texture/quest hàng ngày_nút nhận thưởng 3.png";
        private const string CoinSource =
            "Assets/Module/Quest/Texture/quest hàng ngày_tiền 1.png";
        private const string FrameDestination =
            ResourceRoot + "/currency_frame.png";
        private const string CoinDestination =
            ResourceRoot + "/currency_coin.png";
        private const string HudPrefabPath =
            ResourceRoot + "/CurrencyHud.prefab";

        static CurrencyAssetSetup()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("Tools/Currency/Ensure Currency HUD Prefab")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/Module/Currency", "Resources");
            EnsureFolder("Assets/Module/Currency/Resources", "Currency");
            CopyIfMissing(FrameSource, FrameDestination);
            CopyIfMissing(CoinSource, CoinDestination);
            CreateHudPrefabIfMissing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateHudPrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath) != null)
                return;

            GameObject root = null;
            try
            {
                root = new GameObject(
                    "CurrencyHud",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(CurrencyHudView));

                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 110;

                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                Image frame = CreateImage(
                    "Currency Frame",
                    root.transform,
                    AssetDatabase.LoadAssetAtPath<Sprite>(FrameDestination));
                RectTransform frameRect = frame.rectTransform;
                frameRect.anchorMin = frameRect.anchorMax = new Vector2(0f, 1f);
                frameRect.pivot = new Vector2(0f, 1f);
                frameRect.anchoredPosition = new Vector2(24f, -24f);
                frameRect.sizeDelta = new Vector2(330f, 88f);
                frame.preserveAspect = true;

                Image coin = CreateImage(
                    "Coin",
                    frame.transform,
                    AssetDatabase.LoadAssetAtPath<Sprite>(CoinDestination));
                RectTransform coinRect = coin.rectTransform;
                coinRect.anchorMin = coinRect.anchorMax = new Vector2(0f, 0.5f);
                coinRect.pivot = new Vector2(0f, 0.5f);
                coinRect.anchoredPosition = new Vector2(20f, 0f);
                coinRect.sizeDelta = new Vector2(50f, 50f);
                coin.preserveAspect = true;

                TextMeshProUGUI balanceLabel = CreateBalanceLabel(frame.transform);
                SerializedObject view =
                    new SerializedObject(root.GetComponent<CurrencyHudView>());
                view.FindProperty("_balanceLabel").objectReferenceValue = balanceLabel;
                view.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
                Debug.Log($"[CurrencySetup] Created editable HUD prefab: {HudPrefabPath}");
            }
            finally
            {
                if (root != null)
                    Object.DestroyImmediate(root);
            }
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateBalanceLabel(Transform parent)
        {
            GameObject labelObject = new GameObject(
                "Balance",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(82f, 8f);
            rect.offsetMax = new Vector2(-44f, -8f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "1,000";
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = new Color(0.34f, 0.17f, 0.12f);
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 30f;
            return label;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(destination) != null)
                return;
            if (AssetDatabase.LoadAssetAtPath<Object>(source) == null)
            {
                Debug.LogWarning(
                    $"[CurrencySetup] Source asset is missing: {source}");
                return;
            }
            if (!AssetDatabase.CopyAsset(source, destination))
            {
                Debug.LogError(
                    $"[CurrencySetup] Could not copy {source} to {destination}.");
            }
        }
    }
}
#endif
