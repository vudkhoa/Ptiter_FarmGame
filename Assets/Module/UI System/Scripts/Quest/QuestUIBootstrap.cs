using System.Collections;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.UIManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Shared.Utils;

namespace MyOwn.ServiceHarness
{
    /// <summary>Creates the HUD launcher without changing another developer's scene hierarchy.</summary>
    public sealed class QuestUIBootstrap : MonoBehaviour
    {
        private static QuestUIBootstrap _instance;
        private GameObject _launcherObject;
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
            var bootstrap = new GameObject("[Quest UI Bootstrap]");
            bootstrap.AddComponent<QuestUIBootstrap>();
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
                manager ??=
                    UnityEngine.Object.FindAnyObjectByType<WindowsManager>();
                GameLifetimeScope gameScope =
                    UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();
                if (gameScope != null)
                    _resolver = gameScope.Container;
                yield return null;
            }

            UIWindow questWindow = UIWindowCollection.Values.FirstOrDefault(
                window => window != null && window.name == "QuestWindow");
            if (questWindow == null)
            {
                Debug.LogWarning(
                    "[QuestUI] QuestWindow asset is missing. Run Tools/Quest/Rebuild Quest Content & UI.");
                yield break;
            }

            _launcherObject = new GameObject(
                "[Quest HUD Canvas]",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _launcherObject.transform.SetParent(transform, false);
            Canvas launcherCanvas = _launcherObject.GetComponent<Canvas>();
            launcherCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            launcherCanvas.sortingOrder = 100;
            CanvasScaler scaler = _launcherObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject launcherObject = new GameObject(
                "Quest HUD Launcher",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(WindowOpenTrigger));
            RectTransform rect = (RectTransform)launcherObject.transform;
            rect.SetParent(_launcherObject.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(160f, 64f);

            Image image = launcherObject.GetComponent<Image>();
            image.color = new Color(0.84f, 0.39f, 0.17f, 0.96f);
            Button button = launcherObject.GetComponent<Button>();

            GameObject labelObject = new GameObject(
                "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "NHIỆM VỤ";
            label.fontSize = 25;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            WindowOpenTrigger launcher =
                launcherObject.GetComponent<WindowOpenTrigger>();
            launcher.Configure(manager, questWindow, button, _resolver);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Debug.Log(
                $"[QuestUI] HUD launcher is ready. " +
                $"Canvas={((RectTransform)_launcherObject.transform).rect}, " +
                $"Button={rect.rect}, Position={rect.position}, " +
                $"Scale={rect.lossyScale}, Active={launcherObject.activeInHierarchy}.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (_launcherObject != null)
                UnityEngine.Object.Destroy(_launcherObject);
        }
    }
}
