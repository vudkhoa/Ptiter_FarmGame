using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Wrap PlayerData runtime instance, expose Load/Save/Reset.
    /// POCO Singleton; auto-loads trong StartAsync khi container build.
    /// </summary>
    public sealed class PlayerDataHolder : IService, IAsyncStartable
    {
        private readonly IPublisher<PlayerDataLoadedPayload> _loadedPublisher;
        private PlayerData _data;

        public PlayerData Data => _data;

        /// <summary>True khi Load() KHÔNG tìm thấy save file → tạo PlayerData mặc định.</summary>
        public bool IsNewlyCreated { get; private set; }

        public PlayerDataHolder(IPublisher<PlayerDataLoadedPayload> loadedPublisher)
        {
            _loadedPublisher = loadedPublisher;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            Load();
            return UniTask.CompletedTask;
        }

        /// <summary>Re-init explicit (idempotent) — dùng sau cloud restore hoặc trong test.</summary>
        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Load()
        {
            var loaded = PlayerDataSaveLoad.Load();
            IsNewlyCreated = loaded == null;
            _data = loaded ?? new PlayerData();
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(IsNewlyCreated));
            Debug.Log($"[PlayerDataHolder] Loaded. IsNewlyCreated={IsNewlyCreated}, SaveVersion={_data.SaveVersion}");
        }

        public void Save()
        {
            if (_data == null) return;
            PlayerDataSaveLoad.Save(_data);
        }

        public void Reset()
        {
            _data = new PlayerData();
            PlayerDataSaveLoad.Save(_data);
            _loadedPublisher.Publish(new PlayerDataLoadedPayload(true));
        }
    }
}
