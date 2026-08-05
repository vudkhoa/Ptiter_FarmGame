using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer.Unity;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Cầu nối: module khác publish payload -> gọi service. Đăng ký bằng RegisterEntryPoint.
    /// IInitializable chứ KHÔNG phải IStartable: Initialize() chạy đồng bộ trong Build(),
    /// còn Start() bị đẩy tới EarlyUpdate - lúc đó MonoBehaviour.Start() đã publish xong và mất payload.
    /// </summary>
    public sealed class CutsceneRequestListener : IInitializable, IDisposable
    {
        private readonly ISubscriber<PlayCutsceneRequestPayload> _subscriber;
        private readonly ICutsceneService _service;

        private IDisposable _subscription;

        public CutsceneRequestListener(
            ISubscriber<PlayCutsceneRequestPayload> subscriber,
            ICutsceneService service)
        {
            _subscriber = subscriber;
            _service = service;
        }

        public void Initialize() => _subscription = _subscriber.Subscribe(OnRequested);

        private void OnRequested(PlayCutsceneRequestPayload payload)
            => _service.PlayAsync(payload.CutsceneId).Forget();

        public void Dispose() => _subscription?.Dispose();
    }
}
