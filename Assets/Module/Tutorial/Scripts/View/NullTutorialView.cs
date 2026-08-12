namespace Core.Module.Tutorial
{
    /// <summary>
    /// Stand-in used when the persistent tutorial container is missing from the Preloading scene.
    /// Keeps the container graph resolvable so a missing prefab costs the tutorial, not the boot.
    /// </summary>
    public sealed class NullTutorialView : ITutorialView
    {
        public bool IsShowing => false;

        public void ShowStep(TutorialStepSO step) { }

        public void Hide() { }
    }
}
