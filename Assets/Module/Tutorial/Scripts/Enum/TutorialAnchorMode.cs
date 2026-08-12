namespace Core.Module.Tutorial
{
    /// <summary>How a step decides where on screen the hand belongs.</summary>
    public enum TutorialAnchorMode
    {
        /// <summary>Resolve anchorId through TutorialAnchorRegistry (UI rect, world transform or world point).</summary>
        Anchor = 0,

        /// <summary>Fixed viewport position, 0..1 from the bottom-left. No scene dependency.</summary>
        ScreenPercent = 1,

        /// <summary>
        /// Fixed point in world space, projected through the gameplay camera. Use for a target
        /// that always sits at the same place on the map: unlike Anchor it does not depend on
        /// anything being registered this session, so it survives a restart.
        /// </summary>
        WorldPoint = 2,
    }
}
