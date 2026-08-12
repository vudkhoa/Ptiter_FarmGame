using System;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Per-step hand placement and motion, authored in the TutorialStepSO inspector.
    /// Read-only at runtime: the view copies what it needs and never writes back.
    /// </summary>
    [Serializable]
    public sealed class TutorialHandConfig
    {
        [Header("Target")]
        public TutorialAnchorMode anchorMode = TutorialAnchorMode.Anchor;

        [Tooltip("Id registered by a TutorialAnchor component or by a gameplay bridge.")]
        public string anchorId;

        [Tooltip("Viewport position used when anchorMode is ScreenPercent. (0,0) bottom-left, (1,1) top-right.")]
        public Vector2 screenPercent = new Vector2(0.5f, 0.5f);

        [Tooltip("World position used when anchorMode is WorldPoint. The hand tracks it through the gameplay camera.")]
        public Vector3 worldPosition = Vector3.zero;

        [Header("Placement")]
        [Tooltip("Canvas-space offset applied after the anchor resolves. Moves the hand off the button it points at.")]
        public Vector2 offset = new Vector2(40f, -70f);

        [Tooltip("Z rotation in degrees.")]
        public float rotation;

        [Min(0.01f)] public float scale = 1f;

        public bool flipX;

        [Header("Motion")]
        public TutorialHandAnimation animation = TutorialHandAnimation.Tap;

        [Min(0.05f)] public float animationDuration = 0.6f;

        [Min(0f)] public float animationLoopDelay = 0.15f;

        [Tooltip("Scale multiplier at the peak of Tap / Press / Pulse.")]
        public float animationStrength = 0.82f;

        [Tooltip(
            "Share of the beat spent pressing in; the rest is the release. The highlighted widget " +
            "swells over exactly this window and settles over the rest, so hand and widget stay " +
            "locked in phase.")]
        [Range(0.05f, 0.95f)] public float pressRatio = 0.4f;

        [Tooltip("Easing while the hand presses down and the widget swells.")]
        public Ease pressEase = Ease.InQuad;

        [Tooltip("Easing while the hand lifts and the widget settles back.")]
        public Ease releaseEase = Ease.OutBack;

        [Tooltip("Canvas-space travel for the Swipe animation.")]
        public Vector2 swipeOffset = new Vector2(0f, 180f);
    }
}
