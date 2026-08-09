using UnityEngine;

namespace Core.Module.Audio
{
    [CreateAssetMenu(
        fileName = "AudioCatalog",
        menuName = "Farm Game/Audio/Catalog")]
    public sealed class AudioCatalogSO : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioClip _farmMusic;

        [Header("UI")]
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _success;
        [SerializeField] private AudioClip _error;

        [Header("Farm")]
        [SerializeField] private AudioClip _plant;
        [SerializeField] private AudioClip _water;
        [SerializeField] private AudioClip _harvest;

        [Header("Map")]
        [SerializeField] private AudioClip _placeObject;
        [SerializeField] private AudioClip _removeObject;

        [Header("Quest")]
        [SerializeField] private AudioClip _questComplete;
        [SerializeField] private AudioClip _claimReward;

        public AudioClip FarmMusic => _farmMusic;
        public AudioClip ButtonClick => _buttonClick;
        public AudioClip Success => _success;
        public AudioClip Error => _error;
        public AudioClip Plant => _plant;
        public AudioClip Water => _water;
        public AudioClip Harvest => _harvest;
        public AudioClip PlaceObject => _placeObject;
        public AudioClip RemoveObject => _removeObject;
        public AudioClip QuestComplete => _questComplete;
        public AudioClip ClaimReward => _claimReward;
    }
}
