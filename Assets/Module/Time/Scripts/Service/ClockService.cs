using Cysharp.Threading.Tasks;
using MessagePipe;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace Core.Module.Time
{
    [DisallowMultipleComponent]
    public sealed class ClockService : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _tickIntervalSeconds = 1f;

        // Runtime Service
        private IPublisher<ClockTickPayload> _tickPublisher;
        private IServerTimeProvider _serverTimeProvider;

        private CancellationTokenSource _loopCts;
        private int _tickCount;
        private int _pauseDepth;

        public int TickCount => _tickCount;
        public bool IsPaused => _pauseDepth > 0;

        #region DI - Construct
        [Inject]
        public void Construct(
            IPublisher<ClockTickPayload> tickPublisher, 
            IServerTimeProvider serverTimeProvider)
        {
            _tickPublisher = tickPublisher;
            _serverTimeProvider = serverTimeProvider;
        }
        #endregion

        #region Unity LifeCycle
        private void Start()
        {
            _loopCts = new CancellationTokenSource();
            TickLoopAsync(_loopCts.Token).Forget();
        }

        private void OnDestroy()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }
        #endregion

        #region Tick Logic
        private async UniTask TickLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_tickIntervalSeconds), cancellationToken: ct);

                    // Pause: skip publish, KHÔNG break loop → Resume tự tick lại.
                    if (IsPaused) continue;

                    _tickCount++;
                    Debug.Log($"[Khoa-Debug] Test Tick Logic: {_tickCount}");
                    var payload = new ClockTickPayload(_tickCount, _serverTimeProvider.UtcNow);
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
        #endregion

        #region Tick Control
        public void Pause()
        {
            _pauseDepth++;
        }

        public void Resume()
        {
            _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
        }
        #endregion
    }
}