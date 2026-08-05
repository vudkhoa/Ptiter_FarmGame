using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Nút phát cutscene - đặt được trong bất kỳ prefab nào (màn thành tựu, nút replay, nút test).
    /// Publish payload chứ không gọi thẳng service, giữ đúng hướng phụ thuộc của module.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CutscenePlayButton : MonoBehaviour
    {
        [Tooltip("Phải khớp đúng field cutsceneId trong CutsceneSO.")]
        [SerializeField] private string _cutsceneId;

        [SerializeField] private Button _button;

        private IPublisher<PlayCutsceneRequestPayload> _publisher;

        [Inject]
        public void Construct(IPublisher<PlayCutsceneRequestPayload> publisher) => _publisher = publisher;

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Play);
        }

        /// <summary>Public để gọi được từ onClick khác hoặc từ code.</summary>
        public void Play()
        {
            if (string.IsNullOrWhiteSpace(_cutsceneId))
            {
                Debug.LogError($"[CutscenePlayButton] '{name}' chưa điền cutsceneId.", this);
                return;
            }

            if (_publisher == null)
            {
                Debug.LogError(
                    $"[CutscenePlayButton] '{name}' chưa được inject - thả object vào Auto Inject Game Objects " +
                    "của GameLifetimeScope, hoặc Inject tay window chứa nó sau khi Open.", this);
                return;
            }

            _publisher.Publish(new PlayCutsceneRequestPayload(_cutsceneId));
        }

        private void OnValidate() => _button ??= GetComponent<Button>();
    }
}
