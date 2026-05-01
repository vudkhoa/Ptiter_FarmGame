using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Tick mỗi 1s, publish <see cref="ClockTickPayload"/>. POCO Singleton ở Root → tick xuyên scene.
    /// Move sang GameLifetimeScope với Lifetime.Scoped nếu muốn tick chỉ khi gameplay active.
    /// </summary>
    public sealed class ClockService : IService, IAsyncStartable, IDisposable
    {
        private const float TICK_INTERVAL_SECONDS = 1f;

        private readonly IPublisher<ClockTickPayload> _tickPublisher;
        private CancellationTokenSource _loopCts;
        private int _tickCount;
        private int _pauseDepth;

        public int TickCount => _tickCount;
        public bool IsPaused => _pauseDepth > 0;

        public ClockService(IPublisher<ClockTickPayload> tickPublisher)
        {
            _tickPublisher = tickPublisher;
        }

        /// <summary>Auto-invoked bởi VContainer (IAsyncStartable + AsImplementedInterfaces).</summary>
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // Link container ct với loop CTS → Dispose stop được loop.
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            await TickLoopAsync(_loopCts.Token);
        }

        private async UniTask TickLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(TICK_INTERVAL_SECONDS), cancellationToken: ct);

                    // Pause: skip publish, KHÔNG break loop → Resume tự tick lại.
                    if (IsPaused) continue;

                    _tickCount++;
                    var payload = new ClockTickPayload(_tickCount, DateTime.UtcNow);
                    _tickPublisher.Publish(payload);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected: container dispose / scene unload.
                Debug.Log($"[ClockService] Tick loop cancelled. Final TickCount={_tickCount}.");
            }
            catch (Exception e)
            {
                // Unexpected: fail-loud cho bug logic.
                Debug.LogError($"[ClockService] Tick loop crashed: {e}");
            }
        }

        public void Pause()
        {
            _pauseDepth++;
        }

        public void Resume()
        {
            _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
        }

        public UniTask InitializeAsync(CancellationToken ct = default) => UniTask.CompletedTask;

        public void Dispose()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }
    }
}
