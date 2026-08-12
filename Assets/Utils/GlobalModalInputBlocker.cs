using Core.Module.Input;
using Core.Module.Toast;
using Core.Module.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Utils
{
    /// Promotes the Canvas that owns the active modal above every other Canvas except the tutorial
    /// and toast overlays, and enables one shared raycast shield below the modal window content.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GlobalModalInputBlocker : MonoBehaviour
    {
        private const string BootstrapName = "[Global Modal Input Blocker]";
        private const string RaycastTargetName = "Modal Raycast Target";
        private const int SortingOrderPadding = 10;

        private static GlobalModalInputBlocker _instance;

        private GameObject _raycastTarget;
        private RectTransform _raycastRect;
        private Image _raycastImage;
        private Transform _popupLayer;

        private Canvas _raisedCanvas;
        private bool _originalOverrideSorting;
        private int _originalSortingOrder;

        #region Unity Lifecycle
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

        private void OnEnable()
        {
            GameplayInputBlockRegistry.BlockedStateChanged += Refresh;
            Refresh(GameplayInputBlockRegistry.IsBlocked);
        }

        private void OnDisable()
        {
            GameplayInputBlockRegistry.BlockedStateChanged -= Refresh;
            ReleaseModalLayer();
        }

        private void LateUpdate()
        {
            if (!GameplayInputBlockRegistry.IsBlocked) return;

            Component owner = GameplayInputBlockRegistry.CurrentOwner;
            Transform popupLayer = owner != null ? owner.transform.parent : null;
            if (popupLayer != _popupLayer ||
                _raycastImage == null ||
                !_raycastImage.raycastTarget)
                Refresh(true);
        }

        private void OnDestroy()
        {
            ReleaseModalLayer();
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Event Handlers
        private void Refresh(bool blocked)
        {
            if (!blocked)
            {
                ReleaseModalLayer();
                return;
            }

            Component owner = GameplayInputBlockRegistry.CurrentOwner;
            Transform popupLayer = owner != null ? owner.transform.parent : null;
            if (popupLayer == null) return;

            EnsureRaycastTarget(popupLayer);

            Canvas ownerCanvas = owner.GetComponentInParent<Canvas>();
            RaiseCanvas(ownerCanvas != null ? ownerCanvas.rootCanvas : null);

            // UIManager sorts its known layers first during Awake, so a HUD button parented
            // directly to the manager can land after Popup and steal raycasts above it.
            popupLayer.SetAsLastSibling();
            _raycastTarget.SetActive(true);
            _raycastRect.SetAsFirstSibling();
            _raycastImage.enabled = true;
            _raycastImage.raycastTarget = true;
        }
        #endregion

        #region Private Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            if (_instance != null) return;

            GameObject bootstrap = new GameObject(BootstrapName);
            bootstrap.AddComponent<GlobalModalInputBlocker>();
        }

        private void EnsureRaycastTarget(Transform popupLayer)
        {
            if (_popupLayer == popupLayer && _raycastTarget != null) return;

            _popupLayer = popupLayer;
            Transform existing = FindSharedRaycastTarget(popupLayer);
            if (existing != null)
            {
                _raycastTarget = existing.gameObject;
                _raycastRect = existing as RectTransform;
                _raycastImage = existing.GetComponent<Image>();
            }

            if (_raycastTarget == null ||
                _raycastRect == null ||
                _raycastImage == null)
            {
                _raycastTarget = new GameObject(
                    RaycastTargetName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                _raycastTarget.SetActive(false);
                _raycastRect = (RectTransform)_raycastTarget.transform;
                _raycastImage = _raycastTarget.GetComponent<Image>();
                _raycastRect.SetParent(popupLayer, false);
                _raycastImage.color = new Color(0f, 0f, 0f, 0.001f);
                _raycastImage.maskable = false;
                _raycastImage.canvasRenderer.cullTransparentMesh = false;

                Debug.LogWarning(
                    $"[GlobalModalInputBlocker] '{RaycastTargetName}' was missing under " +
                    $"'{popupLayer.name}'. A runtime fallback was created.", popupLayer);
            }

            if (_raycastRect.parent != popupLayer)
                _raycastRect.SetParent(popupLayer, false);

            _raycastTarget.layer = popupLayer.root.gameObject.layer;
            _raycastRect.anchorMin = Vector2.zero;
            _raycastRect.anchorMax = Vector2.one;
            _raycastRect.offsetMin = Vector2.zero;
            _raycastRect.offsetMax = Vector2.zero;
            _raycastRect.localScale = Vector3.one;
        }

        private static Transform FindSharedRaycastTarget(Transform modalLayer)
        {
            Transform direct = modalLayer.Find(RaycastTargetName);
            if (direct != null) return direct;

            Transform managerRoot = modalLayer.parent;
            if (managerRoot == null) return null;

            Image[] images = managerRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.name == RaycastTargetName)
                    return image.transform;
            }

            return null;
        }

        private void RaiseCanvas(Canvas targetCanvas)
        {
            if (targetCanvas == null || _raisedCanvas == targetCanvas) return;

            RestoreCanvasSorting();

            _raisedCanvas = targetCanvas;
            _originalOverrideSorting = targetCanvas.overrideSorting;
            _originalSortingOrder = targetCanvas.sortingOrder;

            int highestOrder = targetCanvas.sortingOrder;
            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas) continue;
                // Tutorial overlays are lifted ABOVE this modal a few lines down; counting them
                // here would make each side chase the other upwards forever.
                if (TutorialOverlayCanvasRegistry.IsOverlay(canvas)) continue;
                // The toast layer parks at a fixed order far above everything and never moves.
                // Counting it would push each modal past it and hide the message it just raised.
                if (ToastCanvasRegistry.IsOverlay(canvas)) continue;

                highestOrder = Mathf.Max(highestOrder, canvas.sortingOrder);
            }

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = highestOrder + SortingOrderPadding;

            // A tutorial step can point at a widget inside this very window, so its dim, its
            // clone of the widget and its hand all have to clear the window they explain.
            TutorialOverlayCanvasRegistry.RaiseAbove(targetCanvas.sortingOrder);
        }

        private void ReleaseModalLayer()
        {
            if (_raycastImage != null) _raycastImage.raycastTarget = false;

            RestoreCanvasSorting();
        }

        private void RestoreCanvasSorting()
        {
            // Unconditional: the overlays must come back down even if the modal canvas went away
            // with its scene, otherwise the tutorial keeps a raise nothing is holding up any more.
            TutorialOverlayCanvasRegistry.ResetToBase();

            if (_raisedCanvas == null) return;

            _raisedCanvas.overrideSorting = _originalOverrideSorting;
            _raisedCanvas.sortingOrder = _originalSortingOrder;
            _raisedCanvas = null;
        }
        #endregion
    }
}
