namespace Core.Module.Toast
{
    /// The one way to put a transient message on screen. Fire-and-forget: a missing view
    /// degrades to a no-op rather than throwing.
    public interface IToastService
    {
        void Show(string message);

        void Show(string message, ToastStyle style);

        /// Duration 0 falls back to the config default.
        void Show(string message, ToastStyle style, float duration);

        void Show(in ToastRequest request);

        void HideAll();
    }
}
