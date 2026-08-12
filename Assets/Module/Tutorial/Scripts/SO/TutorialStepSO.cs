using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// One instruction beat. RULE: fields are read-only at runtime - progress lives in
    /// TutorialSaveData, never on the asset, or it would leak between play sessions in the editor.
    /// </summary>
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
        [Tooltip("Player-facing text. Leave empty to hide the hint bubble.")]
        [TextArea(1, 3)]
        public string hintText;

        [Tooltip(
            "Canvas-space offset of the hint bubble measured from the BOTTOM OF THE HAND, so it " +
            "stays glued no matter how the hand is scaled or offset. y = -(half the bubble " +
            "height + the gap you want): -90 leaves roughly a 25 unit gap.")]
        public Vector2 hintOffset = new Vector2(0f, -90f);

        [Header("Focus")]
        [Tooltip("Draw the highlight ring around the anchor.")]
        public bool showFocusRing = true;

        [Tooltip("Override the ring artwork, e.g. an isometric diamond for a map cell. Empty uses the default.")]
        public Sprite focusSprite;

        [Tooltip("Breathe the highlighted widget and its ring together, in sync with the hand.")]
        public bool highlightPulse = true;

        [Tooltip("Peak scale of that breath.")]
        [Min(1f)] public float highlightPulseScale = 1.07f;

        [Tooltip(
            "Nudge the ring off the anchor, in canvas units. Independent of the hand: pair it with " +
            "the same value in hand.offset when you want the fingertip to follow the ring.")]
        public Vector2 focusOffset;

        [Tooltip("Extra size added to the resolved anchor rect, in canvas units.")]
        public Vector2 focusPadding = new Vector2(24f, 24f);

        [Tooltip("Fallback focus size when the anchor has no rect (world anchors).")]
        public Vector2 focusFallbackSize = new Vector2(180f, 180f);

        [Header("Input")]
        [Tooltip("Darken the screen behind the highlight.")]
        public bool dimBackground = true;

        [Tooltip(
            "Also make the dim swallow taps, so only the highlighted widget is reachable. " +
            "UI anchors only - a world anchor is tapped through the map raycast, which a " +
            "screen-filling dim would eat, so it stays click-through there no matter what.")]
        public bool blockInputOutsideFocus = true;

        [Tooltip("Anchor ids to hide while this step runs, e.g. a panel that covers the target.")]
        public List<string> hiddenAnchorIds = new List<string>();

        [Header("Flow")]
        [Min(0f)]
        [Tooltip("Delay before the hand appears, so it does not fight a window opening animation.")]
        public float startDelay = 0.35f;

        [Tooltip("Signal that marks this step done. None means the step completes as soon as it is shown.")]
        public TutorialSignal completionSignal = TutorialSignal.None;

        public bool IsValid => !string.IsNullOrWhiteSpace(stepId);
    }
}
