using Core.Module.Cutscene;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Shared.Utils.HUD
{
    /// <summary>HUD action that publishes a cutscene request through MessagePipe.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayCutsceneHudAction : MonoBehaviour, IHudButtonAction
    {
        [Tooltip("Must match cutsceneId in CutsceneSO.")]
        [SerializeField] private string _cutsceneId;

        private IPublisher<PlayCutsceneRequestPayload> _publisher;

        [Inject]
        public void Construct(IPublisher<PlayCutsceneRequestPayload> publisher)
        {
            _publisher = publisher;
        }

        public void Configure(string cutsceneId)
        {
            _cutsceneId = cutsceneId;
        }

        public bool CanExecute()
        {
            return !string.IsNullOrWhiteSpace(_cutsceneId) &&
                   _publisher != null;
        }

        public bool TryExecute()
        {
            if (string.IsNullOrWhiteSpace(_cutsceneId))
            {
                Debug.LogError(
                    $"[PlayCutsceneHudAction] '{name}' has no cutsceneId.",
                    this);
                return false;
            }

            if (_publisher == null)
            {
                Debug.LogError(
                    $"[PlayCutsceneHudAction] '{name}' was not injected. " +
                    "Instantiate or inject the HUD prefab through VContainer.",
                    this);
                return false;
            }

            _publisher.Publish(new PlayCutsceneRequestPayload(_cutsceneId));
            return true;
        }
    }
}
