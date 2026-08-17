using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_water")]
        [SerializeField] private AudioClip _care;
        [SerializeField] private AudioClip _harvest;

        [Header("Map")]
        [SerializeField] private AudioClip _placeObject;
        [SerializeField] private AudioClip _removeObject;

        [Header("Quest")]
        [SerializeField] private AudioClip _questComplete;
        [SerializeField] private AudioClip _claimReward;

        [Header("Economy")]
        [SerializeField] private AudioClip _coin;

        public AudioClip FarmMusic => _farmMusic;
        public AudioClip ButtonClick => _buttonClick;
        public AudioClip Success => _success;
        public AudioClip Error => _error;
        public AudioClip Plant => _plant;
        public AudioClip Care => _care;
        public AudioClip Water => _care;
        public AudioClip Harvest => _harvest;
        public AudioClip PlaceObject => _placeObject;
        public AudioClip RemoveObject => _removeObject;
        public AudioClip QuestComplete => _questComplete;
        public AudioClip ClaimReward => _claimReward;
        public AudioClip Coin => _coin;
    }
}
