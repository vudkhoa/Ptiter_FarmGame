using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// The persistent home of the tutorial UI. Authored into the Preloading scene so the hand
    /// exists before any gameplay scene loads, and kept alive across scene changes - the first
    /// harvest flow fires long after the map has been reloaded a few times.
    /// Delegates to the hand view; it exists so the service depends on a container, not on a prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialUIContainer : MonoBehaviour, ITutorialView
    {
        [Tooltip("Hand view living under this container. Assigned by Tools/Tutorial/Rebuild Tutorial Content.")]
        [SerializeField] private TutorialHandView _handView;

        [Tooltip("Keep this object alive across scene loads. Off only when it already sits under another persistent root.")]
        [SerializeField] private bool _persistAcrossScenes = true;

        public bool IsShowing => _handView != null && _handView.IsShowing;

        private void Awake()
        {
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
    }
}
