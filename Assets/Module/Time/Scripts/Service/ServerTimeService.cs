using Cysharp.Threading.Tasks;
using MessagePipe;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace Core.Module.Time
{
    [DisallowMultipleComponent]
    public sealed class ServerTimeService : MonoBehaviour, IServerTimeProvider
    {
        [Header("Settings")]
        [SerializeField] private TimeServiceConfig _config;

        // Runtime Service
        private ITimeSyncSource _syncSource;
        private IPublisher<ServerTimeSyncedPayload> _syncedPublisher;

        // Runtime Properties
        private TimeSpan _offset;
        private bool _isSynced;
        private DateTime _lastSyncedAt;
        private CancellationTokenSource _loopCts;

        // Constant
        private const string PERSIST_KEY = "time.offsetTicks";

        #region DI - Construct
        [Inject]
        public void Construct(
            ITimeSyncSource syncSource,
            IPublisher<ServerTimeSyncedPayload> syncedPublisher
            )
        {
            _syncSource = syncSource;
            _syncedPublisher = syncedPublisher;
        }
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[ServerTimeService] No Config — service inert.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            _loopCts = new CancellationTokenSource();
            InitializeFlowAsync(_loopCts.Token).Forget();
        }

        private void OnDestroy()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }
        #endregion

        #region Service LifeCycle
        private async UniTaskVoid InitializeFlowAsync(CancellationToken ct)
        {
            LoadPersistedOffset();
            await SyncOnceAsync(ct);
            ResyncLoopAsync(ct).Forget();
        }
        #endregion

        #region IServerTimeProvider - Query API
        public DateTime UtcNow => DateTime.UtcNow + _offset;

        public bool IsSynced => _isSynced;

        public TimeSpan Offset => _offset;

        public DateTime LastSyncedAt => _lastSyncedAt;
        #endregion

        #region Sync Logic
        private async UniTask SyncOnceAsync(CancellationToken ct)
        {
            var result = await _syncSource.SyncAsync(ct);

            if (result.Success)
            {
                _offset = result.Offset;
                _lastSyncedAt = result.SyncedAtUtc;
                _isSynced = true;
                PersistOffset();

                _syncedPublisher.Publish(new ServerTimeSyncedPayload(_offset, _lastSyncedAt));
            }
            else
            {
                Debug.LogWarning($"[ServerTimeService] Sync failed: {result.ErrorMessage}");
            }
        }
        
        private async UniTask ResyncLoopAsync(CancellationToken ct)
        {
            try 
            { 
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_config._resyncIntervalSeconds), cancellationToken: ct);
                    await SyncOnceAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected — OnDestroy cancel CTS lúc app shutdown / scene unload.
                Debug.Log("[ServerTimeService] Resync loop cancelled.");
            }
            catch (Exception e)
            {
                // Unexpected — fail loud.
                Debug.LogError($"[ServerTimeService] Resync loop crashed: {e}");
            }
        }
        #endregion

        #region Persistence
        private void LoadPersistedOffset()
        {
            if (!PlayerPrefs.HasKey(PERSIST_KEY)) return;

            var raw = PlayerPrefs.GetString(PERSIST_KEY);
            if (long.TryParse(raw, out var ticks))
            {
                _offset = TimeSpan.FromTicks(ticks);
            }
            else
            {
                Debug.LogWarning($"[ServerTimeService] Persisted offset corrupt: '{raw}'. Fallback Zero.");
            }
        }

        private void PersistOffset()
        {
            PlayerPrefs.SetString(PERSIST_KEY, _offset.Ticks.ToString());
            PlayerPrefs.Save();
        }
        #endregion
    }
}