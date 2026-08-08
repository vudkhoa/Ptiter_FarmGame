using UnityEngine;

namespace Core.Module.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "Farm Game/Audio/Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioBus _bus = AudioBus.Sfx;
        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        public AudioBus Bus => _bus == AudioBus.Master ? AudioBus.Sfx : _bus;
        public float Volume => Mathf.Clamp01(_volume);

        public AudioClip GetClip()
        {
            if (_clips == null || _clips.Length == 0) return null;
            return _clips[Random.Range(0, _clips.Length)];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _volume = Mathf.Clamp01(_volume);
        }
#endif
    }
}
