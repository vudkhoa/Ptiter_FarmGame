using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// The persistent home of the tutorial UI, authored into the Preloading scene so the hand
    /// outlives every map reload - the first harvest flow fires long after the first load.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialUIContainer : MonoBehaviour, ITutorialView
    {
        [Tooltip("Hand view living under this container. Assigned by Tools/Tutorial/Rebuild Tutorial Content.")]
        [SerializeField] private TutorialHandView _handView;

        [Tooltip("Keep this object alive across scene loads. Off only when it already sits under another persistent root.")]
        [SerializeField] private bool _persistAcrossScenes = true;

        private Canvas[] _overlayCanvases = Array.Empty<Canvas>();

        #region Properties
        public bool IsShowing => _handView != null && _handView.IsShowing;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            RegisterOverlayCanvases();

            if (_handView == null) _handView = GetComponentInChildren<TutorialHandView>(true);

            if (_handView == null)
            {
                Debug.LogError(
                    "[TutorialUIContainer] No TutorialHandView under this container. " +
                    "Run Tools/Tutorial/Rebuild Tutorial Content.", this);
            }
            else
            {
                // The view must start disabled, and it cannot disable itself: its own Awake only
                // runs once something activates it, which is exactly when a step is being shown.
                _handView.gameObject.SetActive(false);
            }

            // Guard the double-persist case: an object already parented under a DontDestroyOnLoad
            // root is moved out of that root by a second DontDestroyOnLoad call.
            if (_persistAcrossScenes && transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _overlayCanvases.Length; i++)
            {
                TutorialOverlayCanvasRegistry.Unregister(_overlayCanvases[i]);
            }
        }
        #endregion

        #region ITutorialView
        public void ShowStep(TutorialStepSO step)
        {
            if (_handView == null) return;
            _handView.ShowStep(step);
        }

        public void Hide()
        {
            if (_handView == null) return;
            _handView.Hide();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Hands the container's canvases to the overlay registry so a focused modal lifts the
        /// whole thing as one, instead of leaving the hand behind the window it points into.
        /// </summary>
        private void RegisterOverlayCanvases()
        {
            // Inactive included: the hand view spends most of its life switched off.
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            List<Canvas> overlays = new List<Canvas>(canvases.Length);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                // A nested canvas that does not override sorting inherits its parent's depth;
                // writing an order onto it would change nothing and mask the one that counts.
                if (!canvas.isRootCanvas && !canvas.overrideSorting) continue;

                TutorialOverlayCanvasRegistry.Register(canvas);
                overlays.Add(canvas);
            }

            _overlayCanvases = overlays.ToArray();
        }
        #endregion
    }
}
