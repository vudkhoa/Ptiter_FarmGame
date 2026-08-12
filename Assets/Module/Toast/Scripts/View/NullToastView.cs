namespace Core.Module.Toast
{
    /// Stand-in used when the persistent container is missing from the Preloading scene, so a
    /// half-set-up project loses its toasts instead of failing to build the container.
    public sealed class NullToastView : IToastView
    {
        public bool IsShowing => false;

        public void Show(in ToastRequest request) { }

        public void HideAll() { }
    }
}
