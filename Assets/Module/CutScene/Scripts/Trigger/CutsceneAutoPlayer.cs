using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Xin phát 1 cutscene khi scene mở. Thả object vào Auto Inject Game Objects của GameLifetimeScope để được inject.
    /// Publish payload chứ không gọi thẳng service, giữ đúng hướng phụ thuộc của module.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneAutoPlayer : MonoBehaviour
    {
        [Tooltip("Phải khớp đúng field cutsceneId trong CutsceneSO.")]
        [SerializeField] private string _cutsceneId;

        [Tooltip("Tắt nếu muốn code khác tự gọi Play().")]
        [SerializeField] private bool _playOnStart = true;

        private IPublisher<PlayCutsceneRequestPayload> _publisher;

        [Inject]
        public void Construct(IPublisher<PlayCutsceneRequestPayload> publisher) => _publisher = publisher;

        private void Start()
        {
            if (_playOnStart) Play();
        }

        public void Play()
        {
            if (string.IsNullOrWhiteSpace(_cutsceneId))
            {
                Debug.LogError($"[CutsceneAutoPlayer] '{name}' chưa điền cutsceneId.");
                return;
            }

            if (_publisher == null)
            {
                Debug.LogError(
                    $"[CutsceneAutoPlayer] '{name}' chưa được inject - thả object vào Auto Inject Game Objects của GameLifetimeScope.");
                return;
            }

            _publisher.Publish(new PlayCutsceneRequestPayload(_cutsceneId));
        }
    }
}
