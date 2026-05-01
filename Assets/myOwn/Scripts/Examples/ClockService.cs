using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// EXAMPLE service: tick mỗi 1 giây, publish <see cref="ClockTickPayload"/>.
    /// Demo:
    /// - Constructor injection (POCO) để nhận IPublisher.
    /// - VContainer auto-call StartAsync khi container build xong (do implement IAsyncStartable).
    /// - UniTask.Delay loop với CancellationToken (auto-cancel khi container dispose).
    /// - Publisher pattern thay cho static GlobalMessagePipe.GetPublisher.
    /// </summary>
    /// <remarks>
    /// Lifetime: Singleton (registered ở RootLifetimeScope) → tick chạy xuyên scene.
    /// Nếu muốn tick chỉ khi gameplay scene active → move sang GameLifetimeScope với Lifetime.Scoped.
    /// </remarks>
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

        /// <summary>
        /// VContainer auto-invoke method này sau khi container build xong.
        /// Đây là entry-point để start tick loop.
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // Link container's cancellation với loop's own CTS để Pause/Resume/Dispose đều stop được loop.
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

                    // Pause: skip publish nhưng vẫn delay tiếp (KHÔNG break loop) → Resume tự tick lại.
                    if (IsPaused) continue;

                    _tickCount++;
                    var payload = new ClockTickPayload(_tickCount, DateTime.UtcNow);
                    _tickPublisher.Publish(payload);
                    Debug.Log($"[Khoa-Debug] Clock Service Time: {_tickCount}");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected: container dispose / scene unload → UniTask.Delay throw OCE. Không phải lỗi.
                Debug.Log($"[ClockService] Tick loop cancelled. Final TickCount={_tickCount}.");
            }
            catch (Exception e)
            {
                // Unexpected: bug logic / null ref / publisher fail → log Error để fail-loud.
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
