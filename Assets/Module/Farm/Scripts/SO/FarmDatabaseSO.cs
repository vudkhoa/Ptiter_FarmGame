using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "FarmDatabase", menuName = "GDD/Farm/Farm Database")]
    public class FarmDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<FarmEntityData> allEntities = new List<FarmEntityData>();

        public IReadOnlyList<FarmEntityData> AllEntities => allEntities;

        private Dictionary<string, FarmEntityData> _entityCache;

        public void InitializeLookups()
        {
            _entityCache = new Dictionary<string, FarmEntityData>();
            foreach (var entity in allEntities)
            {
                if (entity != null && !_entityCache.ContainsKey(entity.EntityId))
                    _entityCache.Add(entity.EntityId, entity);
            }
        }

        public FarmEntityData GetEntityById(string id)
        {
            if (_entityCache == null) InitializeLookups();
            return _entityCache.TryGetValue(id, out var entity) ? entity : null;
        }
    }
}
