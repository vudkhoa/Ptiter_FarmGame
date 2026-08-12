namespace Core.Module.Tutorial
{
    /// <summary>Looping motion the hand plays while a step is active.</summary>
    public enum TutorialHandAnimation
    {
        /// <summary>Static hand - position and rotation only.</summary>
        None = 0,

        /// <summary>Scale punch, the classic "tap here" beat.</summary>
        Tap = 1,

        /// <summary>Slow scale down and hold, for press-and-hold interactions.</summary>
        Press = 2,

        /// <summary>Travels from the anchor along swipeOffset and restarts.</summary>
        Swipe = 3,

        /// <summary>Gentle alpha + scale breathing, for "look here" without a tap.</summary>
        Pulse = 4,
    }
}
