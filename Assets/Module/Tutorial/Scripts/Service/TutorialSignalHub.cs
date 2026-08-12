using UnityEngine;

namespace Core.Module.Tutorial
{
    /// Signal channel for UI only: UIManager opens a window before VContainer injects it, so a
    /// screen cannot report through an injected field. Gameplay goes via TutorialGameplaySignalBridge.
    public static class TutorialSignalHub
    {
        private static ITutorialService _service;

        #region Public API
        public static void Bind(ITutorialService service) => _service = service;

        /// Only the owner may unbind, so a stale service cannot detach its replacement.
        public static void Unbind(ITutorialService service)
        {
            if (_service == service) _service = null;
        }

        /// No-op until the service is up - reporting before boot is normal, not an error.
        public static void Report(TutorialSignal signal)
        {
            if (signal == TutorialSignal.None || _service == null) return;

            _service.ReportSignal(signal);
        }
        #endregion

        #region Private Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _service = null;
        #endregion
    }
}
