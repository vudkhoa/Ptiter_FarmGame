namespace Core.Module.Tutorial
{
    public interface ITutorialService
    {
        /// <summary>True while a step is on screen waiting for the player.</summary>
        bool IsRunning { get; }

        string CurrentFlowId { get; }

        string CurrentStepId { get; }

        /// <summary>
        /// Feed a gameplay moment in. Signals that no active step cares about are ignored,
        /// so bridges can report everything without knowing the current step.
        /// </summary>
        void ReportSignal(TutorialSignal signal);

        /// <summary>Wipe saved progress and restart from the first flow. Debug and QA only.</summary>
        void ResetProgress();
    }
}
