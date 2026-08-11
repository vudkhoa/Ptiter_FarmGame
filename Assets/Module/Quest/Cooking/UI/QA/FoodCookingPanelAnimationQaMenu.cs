#if UNITY_EDITOR
using Core.Module.Quest.Cooking.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest.Cooking.QA
{
    [InitializeOnLoad]
    public static class FoodCookingPanelAnimationQaMenu
    {
        private const string HostName =
            "[Cooking UI QA Preview]";
        private const string RequestedKey =
            "Quest.Cooking.AnimationQa.Requested";
        private const string PrefabPath =
            "Assets/Module/Quest/Cooking/UI/" +
            "FoodCookingPanel.prefab";

        static FoodCookingPanelAnimationQaMenu()
        {
            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged;
        }

        [MenuItem(
            "Tools/Quest/Cooking/Preview Animation",
            priority = 2300)]
        public static void PreviewAnimation()
        {
            SessionState.SetBool(RequestedKey, true);

            if (EditorApplication.isPlaying)
            {
                CreatePreview();
                return;
            }

            EditorApplication.EnterPlaymode();
        }

        [MenuItem(
            "Tools/Quest/Cooking/Stop Preview",
            priority = 2301)]
        public static void StopPreview()
        {
            SessionState.SetBool(RequestedKey, false);
            DestroyPreview();
        }

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                SessionState.GetBool(RequestedKey, false))
            {
                CreatePreview();
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
                SessionState.SetBool(RequestedKey, false);
        }

        private static void CreatePreview()
        {
            if (!EditorApplication.isPlaying)
                return;

            DestroyPreview();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[Cooking QA] Missing prefab at {PrefabPath}.");
                return;
            }

            var host = new GameObject(
                HostName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            host.hideFlags = HideFlags.DontSave;
            Object.DontDestroyOnLoad(host);

            RectTransform hostRect =
                (RectTransform)host.transform;
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;

            Canvas canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            CanvasScaler scaler =
                host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = Object.Instantiate(
                prefab,
                host.transform,
                false);
            panel.name = "FoodCookingPanel [Mock Animation]";

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            FoodCookingPanelView view =
                panel.GetComponent<FoodCookingPanelView>();
            FoodCookingPanelAnimationQaHarness harness =
                host.AddComponent<
                    FoodCookingPanelAnimationQaHarness>();
            harness.Configure(
                view,
                quantity: 3,
                countdownSeconds: 5,
                loop: true);

            Selection.activeGameObject = host;
            EditorApplication.ExecuteMenuItem(
                "Window/General/Game");
        }

        private static void DestroyPreview()
        {
            GameObject host = GameObject.Find(HostName);
            if (host != null)
                Object.DestroyImmediate(host);
        }
    }
}
#endif
