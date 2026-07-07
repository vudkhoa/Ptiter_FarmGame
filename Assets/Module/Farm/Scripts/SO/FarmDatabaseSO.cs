using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "FarmDatabase", menuName = "GDD/Farm/Farm Database")]
    public class FarmDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<CropData> allCrops = new List<CropData>();
        [SerializeField] private List<AnimalData> allAnimals = new List<AnimalData>();

        public IReadOnlyList<CropData> AllCrops => allCrops;
        public IReadOnlyList<AnimalData> AllAnimals => allAnimals;

        private Dictionary<string, CropData> _cropCache;
        private Dictionary<string, AnimalData> _animalCache;

        public void InitializeLookups()
        {
            _cropCache = new Dictionary<string, CropData>();
            foreach (var crop in allCrops)
            {
                if (crop != null && !_cropCache.ContainsKey(crop.cropId))
                    _cropCache.Add(crop.cropId, crop);
            }

            _animalCache = new Dictionary<string, AnimalData>();
            foreach (var animal in allAnimals)
            {
                if (animal != null && !_animalCache.ContainsKey(animal.animalId))
                    _animalCache.Add(animal.animalId, animal);
            }
        }

        public CropData GetCropById(string id)
        {
            if (_cropCache == null) InitializeLookups();
            return _cropCache.TryGetValue(id, out var crop) ? crop : null;
        }

        public AnimalData GetAnimalById(string id)
        {
            if (_animalCache == null) InitializeLookups();
            return _animalCache.TryGetValue(id, out var animal) ? animal : null;
        }
    }
}
