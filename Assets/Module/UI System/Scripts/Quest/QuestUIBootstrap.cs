using System.Collections;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.UIManager;
using Core.Module.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>Creates the HUD launcher without changing another developer's scene hierarchy.</summary>
    public sealed class QuestUIBootstrap : MonoBehaviour
    {
        private GameObject _launcherObject;
        private IObjectResolver _resolver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            var bootstrap = new GameObject("[Quest UI Bootstrap]");
            DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<QuestUIBootstrap>();
        }

        private IEnumerator Start()
        {
            WindowsManager manager = null;
            while (manager == null || !TryFindGameResolver(out _resolver))
            {
                manager = UnityEngine.Object.FindAnyObjectByType<WindowsManager>();
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

            Canvas canvas = manager.GetComponentInParent<Canvas>() ??
                            manager.GetComponentInChildren<Canvas>();
            if (canvas == null) yield break;

            _launcherObject = new GameObject(
                "Quest HUD Launcher",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(QuestHudLauncher));
            RectTransform rect = (RectTransform)_launcherObject.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(160f, 64f);

            Image image = _launcherObject.GetComponent<Image>();
            image.color = new Color(0.84f, 0.39f, 0.17f, 0.96f);
            Button button = _launcherObject.GetComponent<Button>();

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

            QuestHudLauncher launcher = _launcherObject.GetComponent<QuestHudLauncher>();
            launcher.Configure(button, manager, questWindow, _resolver);
        }

        private static bool TryFindGameResolver(out IObjectResolver resolver)
        {
            LifetimeScope[] scopes =
                UnityEngine.Object.FindObjectsByType<LifetimeScope>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++)
            {
                IObjectResolver candidate = scopes[i].Container;
                if (candidate == null) continue;
                try
                {
                    if (candidate.Resolve<IDailyQuestService>() != null)
                    {
                        resolver = candidate;
                        return true;
                    }
                }
                catch (VContainerException)
                {
                    // This is a root/other scope, not the gameplay scope.
                }
            }
            resolver = null;
            return false;
        }

        private void OnDestroy()
        {
            if (_launcherObject != null)
                UnityEngine.Object.Destroy(_launcherObject);
        }
    }
}
