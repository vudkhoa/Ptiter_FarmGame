using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// One instruction beat. RULE: fields are read-only at runtime - progress lives in
    /// TutorialSaveData, never on the asset, or it would leak between editor play sessions.
    [CreateAssetMenu(
        fileName = "TutorialStep",
        menuName = "Game/Tutorial/Tutorial Step",
        order = 1)]
    public sealed class TutorialStepSO : ScriptableObject
    {
        [Tooltip("Stable id persisted in the save file. Renaming it replays the step.")]
        public string stepId;

        [Header("Hand")]
        public TutorialHandConfig hand = new TutorialHandConfig();

        [Header("Hint")]
        [Tooltip("Leave empty to hide the hint bubble.")]
        [TextArea(1, 3)]
        public string hintText;

        [Tooltip("Canvas-space offset measured from the BOTTOM OF THE HAND. -90 leaves a ~25 unit gap.")]
        public Vector2 hintOffset = new Vector2(0f, -90f);

        [Header("Focus")]
        public bool showFocusRing = true;

        [Tooltip("Override the ring artwork, e.g. an isometric diamond for a map cell.")]
        public Sprite focusSprite;

        [Tooltip("Breathe the highlighted widget and its ring together, in sync with the hand.")]
        public bool highlightPulse = true;

        [Min(1f)] public float highlightPulseScale = 1.07f;

        [Tooltip("Nudge the ring off the anchor, in canvas units. Independent of the hand.")]
        public Vector2 focusOffset;

        [Tooltip("Extra size added to the resolved anchor rect, in canvas units.")]
        public Vector2 focusPadding = new Vector2(24f, 24f);

        [Tooltip("Fallback focus size when the anchor has no rect (world anchors).")]
        public Vector2 focusFallbackSize = new Vector2(180f, 180f);

        [Header("Input")]
        [Tooltip("Darken the screen behind the highlight.")]
        public bool dimBackground = true;

        [Tooltip("The dim swallows every tap except the anchor. Requires dimBackground.")]
        public bool blockInputOutsideFocus = true;

        [Tooltip("Anchor ids to hide while this step runs, e.g. a panel that covers the target.")]
        public List<string> hiddenAnchorIds = new List<string>();

        [Header("Flow")]
        [Min(0f)]
        [Tooltip("Delay before the hand appears, so it does not fight a window opening animation.")]
        public float startDelay = 0.35f;

        [Tooltip("None means the step completes as soon as it is shown.")]
        public TutorialSignal completionSignal = TutorialSignal.None;

        public bool IsValid => !string.IsNullOrWhiteSpace(stepId);
    }
}
