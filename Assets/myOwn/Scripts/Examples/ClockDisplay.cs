using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// EXAMPLE UI subscriber: hiển thị tick count nhận từ ClockService.
    /// Demo:
    /// - Method Injection ([Inject] vào method Construct) cho MonoBehaviour — MonoBehaviour không có constructor.
    /// - Subscribe ISubscriber + AddTo(this) để auto-dispose khi GameObject destroyed.
    /// </summary>
    /// <remarks>
    /// VContainer cần "Auto Inject GameObjects" enabled trên LifetimeScope (default true)
    /// HOẶC scope.Container.InjectGameObject(this.gameObject) thủ công.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ClockDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private IDisposable _subscription;

        /// <summary>
        /// Method Injection — VContainer call method này khi GameObject inject.
        /// Tên method bất kỳ, chỉ cần có [Inject] attribute.
        /// </summary>
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
