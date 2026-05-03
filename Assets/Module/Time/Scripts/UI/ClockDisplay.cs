using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace Core.Module.Time
{
    /// <summary>
    /// UI subscriber: render ClockTickPayload lên TMP. Method Injection vì MonoBehaviour không có constructor.
    /// Cần LifetimeScope's "Auto Inject GameObjects" = true (default) để VContainer scan + inject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClockDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private IDisposable _subscription;

        [Inject]
        public void Construct(ISubscriber<ClockTickPayload> tickSubscriber)
        {
            _subscription = tickSubscriber.Subscribe(OnTick);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        private void OnTick(ClockTickPayload payload)
        {
            if (_label == null) return;
            _label.text = $"Ticks: {payload.TickCount}\n{payload.UtcNow:HH:mm:ss}";
        }
    }
}
