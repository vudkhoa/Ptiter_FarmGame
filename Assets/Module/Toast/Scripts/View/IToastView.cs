namespace Core.Module.Toast
{
    /// Rendering half of the module. Owns stacking, pooling and animation.
    public interface IToastView
    {
        bool IsShowing { get; }

        void Show(in ToastRequest request);

        void HideAll();
    }
}
