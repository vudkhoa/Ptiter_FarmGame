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
        [SerializeField] private TimeServiceConfig _config;

        // Runtime Service
        private IPublisher<ClockTickPayload> _tickPublisher;
        private IServerTimeProvider _serverTimeProvider;
        private IPublisher<ClockManipulationDetectedPayload> _cheatPublisher;

        private CancellationTokenSource _loopCts;
        private int _tickCount;
        private int _pauseDepth;

        // Track time for real-time drift checking
        private DateTime _lastTickTimeUtc;
        private float _lastTickUnscaledTime;

        public int TickCount => _tickCount;
        public bool IsPaused => _pauseDepth > 0;

        #region DI - Construct
        [Inject]
        public void Construct(
            IPublisher<ClockTickPayload> tickPublisher, 
            IServerTimeProvider serverTimeProvider,
            IPublisher<ClockManipulationDetectedPayload> cheatPublisher)
        {
            _tickPublisher = tickPublisher;
            _serverTimeProvider = serverTimeProvider;
            _cheatPublisher = cheatPublisher;
        }
        #endregion

        #region Unity LifeCycle
        private void Start()
        {
            _loopCts = new CancellationTokenSource();
            _lastTickTimeUtc = DateTime.UtcNow;
            _lastTickUnscaledTime = UnityEngine.Time.unscaledTime;

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

                    // Pause: skip publish, KHÔNG break loop
                    if (IsPaused) continue;

                    DateTime nowUtc = DateTime.UtcNow;
                    float nowUnscaled = UnityEngine.Time.unscaledTime;

                    // Real-time drift check
                    if (_tickCount > 0)
                    {
                        float unscaledDelta = nowUnscaled - _lastTickUnscaledTime;
                        double systemDelta = (nowUtc - _lastTickTimeUtc).TotalSeconds;

                        // Check if system clock jumped forward/backward compared to unscaled processor time
                        double drift = Math.Abs(systemDelta - unscaledDelta);
                        float threshold = _config != null ? _config._driftThresholdSeconds : 10f;

                        // Tick-by-tick threshold is usually smaller to prevent small edits, e.g. 5s
                        float tickThreshold = Mathf.Min(5f, threshold);

                        if (drift > tickThreshold || systemDelta < 0)
                        {
                            Debug.LogError($"[ANTI-CHEAT] Real-time clock jump detected! Drift: {drift}s, Delta: {systemDelta}s");
                            _cheatPublisher.Publish(new ClockManipulationDetectedPayload(
                                _lastTickTimeUtc.AddSeconds(unscaledDelta), 
                                nowUtc
                            ));
                        }
                    }

                    _lastTickUnscaledTime = nowUnscaled;
                    _lastTickTimeUtc = nowUtc;

                    _tickCount++;
                    var payload = new ClockTickPayload(_tickCount, _serverTimeProvider.UtcNow);
                    _tickPublisher.Publish(payload);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ClockService] Tick loop cancelled. Final TickCount={_tickCount}.");
            }
            catch (Exception e)
            {
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