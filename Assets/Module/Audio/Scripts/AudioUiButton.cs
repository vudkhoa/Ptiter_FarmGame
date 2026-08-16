using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Module.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class AudioUiButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        private Button _button;
        private void Awake() => _button = GetComponent<Button>();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_button.IsInteractable())
                AudioUiFeedback.PlayClick(_volume);
            else
                AudioUiFeedback.PlayError();
        }
    }
}
