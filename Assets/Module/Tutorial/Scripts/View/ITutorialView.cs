namespace Core.Module.Tutorial
{
    /// <summary>
    /// What TutorialService is allowed to ask of the hand UI. Deliberately tiny: the service
    /// owns "which step", the view owns "where on screen and how it moves".
    /// </summary>
    public interface ITutorialView
    {
        bool IsShowing { get; }

        void ShowStep(TutorialStepSO step);

        void Hide();
    }
}
