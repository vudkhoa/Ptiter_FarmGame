using System;
using VContainer.Unity;

namespace Core.Module.Toast
{
    /// Validates requests and hands them to the view. Entry point so it exists from container
    /// build, which is what lets ToastHub answer callers that never inject it.
    public sealed class ToastService : IToastService, IStartable, IDisposable
    {
        private readonly IToastView _view;

        private bool _disposed;

        public ToastService(IToastView view)
        {
            _view = view;
        }

        #region Lifecycle
        public void Start() => ToastHub.Bind(this);

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            ToastHub.Unbind(this);
            _view?.HideAll();
        }
        #endregion

        #region Public API
        public void Show(string message)
            => Show(new ToastRequest(message));

        public void Show(string message, ToastStyle style)
            => Show(new ToastRequest(message, style));

        public void Show(string message, ToastStyle style, float duration)
            => Show(new ToastRequest(message, style, duration));

        public void Show(in ToastRequest request)
        {
            if (_disposed || !request.IsValid) return;

            _view?.Show(request);
        }

        public void HideAll()
        {
            if (_disposed) return;

            _view?.HideAll();
        }
        #endregion
    }
}
