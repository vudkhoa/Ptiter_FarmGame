using UnityEngine;

namespace Core.Module.Toast
{
    /// Every tunable of the toast stack. Authored once and shared by the whole game so a
    /// notification looks the same wherever it is raised from.
    [CreateAssetMenu(fileName = "ToastConfig", menuName = "GDD/Toast/Toast Config")]
    public sealed class ToastConfigSO : ScriptableObject
    {
        /// Stamped by ToastProjectSetup. On the committed asset rather than in EditorPrefs so the
        /// regenerate happens once for the repo, not once per machine.
        [HideInInspector] public int setupVersion;

        [Header("Timing (unscaled seconds)")]
        [Tooltip("Hold time used when a caller passes duration 0.")]
        [Min(0.1f)] public float defaultDuration = 1.6f;
        [Min(0f)] public float fadeInDuration = 0.16f;
        [Min(0f)] public float fadeOutDuration = 0.22f;

        [Header("Motion")]
        [Tooltip("Canvas units the toast rises while fading in.")]
        public float riseDistance = 40f;
        [Range(0.5f, 1f)] public float popScale = 0.92f;

        [Header("Stacking")]
        [Tooltip("Toasts on screen at once. Anything past this waits in the queue.")]
        [Min(1)] public int maxVisible = 3;
        [Tooltip("Requests held back while the stack is full. Older ones are dropped first.")]
        [Min(0)] public int maxQueued = 8;

        [Tooltip("Re-showing the same text while it is still up restarts it instead of stacking a copy.")]
        public bool mergeDuplicates = true;

        [Header("Palette")]
        [Tooltip("Off = the prefab keeps whatever background colour it was authored with.")]
        public bool overrideBackgroundColor = true;

        [Tooltip("Alpha applied to every style colour.")]
        [Range(0f, 1f)] public float backgroundOpacity = 0.78f;

        public Color textColor = Color.white;
        public Color infoColor = new Color(0.09f, 0.07f, 0.05f, 1f);
        public Color successColor = new Color(0.10f, 0.42f, 0.16f, 1f);
        public Color warningColor = new Color(0.42f, 0.24f, 0.04f, 1f);
        public Color errorColor = new Color(0.48f, 0.12f, 0.11f, 1f);

        #region Public API
        /// Style tint at the shared opacity. The alpha authored on the colour is ignored.
        public Color ColorFor(ToastStyle style)
        {
            Color color;
            switch (style)
            {
                case ToastStyle.Success: color = successColor; break;
                case ToastStyle.Warning: color = warningColor; break;
                case ToastStyle.Error: color = errorColor; break;
                default: color = infoColor; break;
            }

            color.a = backgroundOpacity;
            return color;
        }

        public float ResolveDuration(float requested) => requested > 0f ? requested : defaultDuration;
        #endregion
    }
}
