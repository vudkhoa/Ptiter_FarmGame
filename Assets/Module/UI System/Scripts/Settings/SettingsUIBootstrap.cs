using System.Collections;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.UIManager;
using MyOwn.ServiceHarness;
using Shared.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Settings.UI
{
    /// <summary>Creates the settings HUD launcher without modifying scene hierarchy.</summary>
    public sealed class SettingsUIBootstrap : MonoBehaviour
    {
        private static SettingsUIBootstrap _instance;
        private GameObject _launcherCanvas;
        private IObjectResolver _resolver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            if (_instance != null) return;
            var bootstrap = new GameObject("[Settings UI Bootstrap]");
            bootstrap.AddComponent<SettingsUIBootstrap>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            WindowsManager manager = null;
            while (manager == null || _resolver == null)
            {
                manager ??= Object.FindAnyObjectByType<WindowsManager>();
                GameLifetimeScope gameScope =
                    Object.FindAnyObjectByType<GameLifetimeScope>();
                if (gameScope != null)
                    _resolver = gameScope.Container;
                yield return null;
            }

            UIWindow settingsWindow = UIWindowCollection.Values.FirstOrDefault(
                window => window != null && window.name == "SettingsWindow");
            if (settingsWindow == null)
            {
                Debug.LogWarning(
                    "[SettingsUI] SettingsWindow is missing. " +
                    "Run Tools/Settings/Rebuild Settings UI.");
                yield break;
            }

            _launcherCanvas = new GameObject(
                "[Settings HUD Canvas]",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _launcherCanvas.transform.SetParent(transform, false);

            Canvas canvas = _launcherCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = _launcherCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject launcherObject = new GameObject(
                "Settings HUD Launcher",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(WindowOpenTrigger));
            RectTransform rect = (RectTransform)launcherObject.transform;
            rect.SetParent(_launcherCanvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -108f);
            rect.sizeDelta = new Vector2(176f, 64f);

            Image image = launcherObject.GetComponent<Image>();
            image.color = new Color(0.47f, 0.26f, 0.20f, 0.97f);
            Button button = launcherObject.GetComponent<Button>();

            GameObject labelObject = new GameObject(
                "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "CÀI ĐẶT";
            label.fontSize = 24;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Itim SDF");
            if (font != null) label.font = font;

            WindowOpenTrigger launcher =
                launcherObject.GetComponent<WindowOpenTrigger>();
            launcher.Configure(manager, settingsWindow, button, _resolver);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_launcherCanvas != null) Destroy(_launcherCanvas);
        }
    }
}
