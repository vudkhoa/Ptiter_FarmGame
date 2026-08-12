using UnityEngine;

namespace Core.Module.Toast
{
    /// Static front door for callers that cannot inject: UIManager opens a window before
    /// VContainer injects it, so a screen's service field is still null at that moment.
    public static class ToastHub
    {
        private static IToastService _service;

        #region Properties
        public static bool IsReady => _service != null;
        #endregion

        #region Public API
        public static void Bind(IToastService service) => _service = service;

        /// Only the owner may unbind, so a stale service cannot detach its replacement.
        public static void Unbind(IToastService service)
        {
            if (_service == service) _service = null;
        }

        /// No-op until the service is up - a toast raised during boot is dropped, not an error.
        public static void Show(string message)
            => _service?.Show(message);

        public static void Show(string message, ToastStyle style)
            => _service?.Show(message, style);

        public static void Show(string message, ToastStyle style, float duration)
            => _service?.Show(message, style, duration);

        public static void Show(in ToastRequest request)
            => _service?.Show(request);

        public static void HideAll() => _service?.HideAll();
        #endregion

        #region Private Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _service = null;
        #endregion
    }
}
