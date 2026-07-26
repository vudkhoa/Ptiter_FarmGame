namespace Core.Module.Loading
{
    // Payload Progress Change
    public class LoadingProgressPayload
    {
        public readonly LoadingPhase Phase;
        public readonly int Progress;
        public readonly string Message;


        public LoadingProgressPayload(LoadingPhase phase, int progress, string message)
        {
            Phase = phase;
            Progress = progress;
            Message = message;
        }
    }
}