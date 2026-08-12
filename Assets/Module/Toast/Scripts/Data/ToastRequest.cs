namespace Core.Module.Toast
{
    /// A struct so the common Show(string) path allocates nothing.
    /// Duration 0 means "use the config default", so callers never have to know the timing.
    public readonly struct ToastRequest
    {
        public const float UseConfigDuration = 0f;

        public readonly string Message;
        public readonly ToastStyle Style;
        public readonly float Duration;

        public ToastRequest(
            string message,
            ToastStyle style = ToastStyle.Info,
            float duration = UseConfigDuration)
        {
            Message = message;
            Style = style;
            Duration = duration;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Message);
    }
}
