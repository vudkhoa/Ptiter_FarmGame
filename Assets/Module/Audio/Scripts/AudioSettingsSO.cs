using UnityEngine;

namespace Core.Module.Audio
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Farm Game/Audio/Settings")]
    public sealed class AudioSettingsSO : ScriptableObject
    {
        [Header("Default volume")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        [Header("SFX")]
        [SerializeField, Min(1)] private int _sfxPoolSize = 8;

        public int SfxPoolSize => Mathf.Max(1, _sfxPoolSize);

        public float GetDefaultVolume(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => _masterVolume,
                AudioBus.Music => _musicVolume,
                AudioBus.Sfx => _sfxVolume,
                _ => 1f
            };
        }
    }
}
