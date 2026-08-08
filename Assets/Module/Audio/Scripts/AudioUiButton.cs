using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class AudioUiButton : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        private Button _button;
        private IAudioService _audio;
        private AudioCatalogSO _catalog;

        [Inject]
        public void Construct(
            IAudioService audio,
            AudioCatalogSO catalog)
        {
            _audio = audio;
            _catalog = catalog;
        }

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Play);
        }

        public void Play()
        {
            if (_audio != null && _catalog != null)
                _audio.PlaySfx(_catalog.ButtonClick, _volume);
        }
    }
}
