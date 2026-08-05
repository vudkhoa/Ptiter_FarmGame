using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Cutscene
{
    [DisallowMultipleComponent]
    public sealed class CutsceneSkipInput : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button _skipButton;

        private ICutsceneService _service;

        [Inject]
        public void Construct(ICutsceneService service) => _service = service;

        private void OnEnable()
        {
            if (_skipButton != null) _skipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnDisable()
        {
            if (_skipButton != null) _skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        // Tap vào vùng màn hình = qua khung; nút Skip = huỷ cả cutscene.
        public void OnPointerClick(PointerEventData eventData) => _service?.RequestNextStep();

        private void OnSkipClicked() => _service?.Skip();
    }
}
