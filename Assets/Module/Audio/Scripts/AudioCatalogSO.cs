using System.Collections.Generic;
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
        [SerializeField] private List<AudioClip> _bgm = new List<AudioClip>();
        [HideInInspector]
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

        public IReadOnlyList<AudioClip> Bgm => _bgm;
        public AudioClip FarmMusic => GetFirstBgm();
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

        public AudioClip GetRandomBgm(AudioClip excludedClip = null)
        {
            AudioClip selected = null;
            int candidateCount = 0;

            for (int i = 0; i < _bgm.Count; i++)
            {
                AudioClip clip = _bgm[i];
                if (clip == null || clip == excludedClip) continue;

                candidateCount++;
                if (Random.Range(0, candidateCount) == 0)
                    selected = clip;
            }

            if (selected != null) return selected;

            // When the catalog contains only one valid clip, replay it.
            return GetFirstBgm();
        }

        private AudioClip GetFirstBgm()
        {
            for (int i = 0; i < _bgm.Count; i++)
            {
                if (_bgm[i] != null)
                    return _bgm[i];
            }

            // Keeps catalogs created before the BGM list was introduced working.
            return _farmMusic;
        }
    }
}
