using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Drop this on any UI element or world object the tutorial hand must point at,
    /// then reference the same id from a TutorialStepSO. Works on prefabs: registration
    /// is by lifecycle, not by scene wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialAnchor : MonoBehaviour
    {
        [SerializeField] private string _anchorId;

        [Tooltip("Point at this child instead of the object itself. Optional.")]
        [SerializeField] private Transform _target;

        public string AnchorId => _anchorId;

        private Transform Target => _target != null ? _target : transform;

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(_anchorId))
            {
                Debug.LogWarning($"[TutorialAnchor] '{name}' has no anchor id and will never resolve.", this);
                return;
            }

            TutorialAnchorRegistry.Register(_anchorId, Target);
        }

        private void OnDisable() => TutorialAnchorRegistry.Unregister(_anchorId, Target);
    }
}
