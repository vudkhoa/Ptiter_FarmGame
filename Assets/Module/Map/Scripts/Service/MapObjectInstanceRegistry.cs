using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    public sealed class MapObjectInstanceRegistry : IMapObjectInstanceRegistry
    {
        private readonly Dictionary<Vector3Int, GameObject> _instances = new();

        public void Register(Vector3Int originCell, GameObject instance)
        {
            if (instance == null) return;
            _instances[originCell] = instance;
        }

        public bool TryGetAtOrigin(Vector3Int originCell, out GameObject instance)
        {
            if (_instances.TryGetValue(originCell, out instance) && instance != null)
                return true;

            _instances.Remove(originCell);
            instance = null;
            return false;
        }

        public void Unregister(Vector3Int originCell)
        {
            _instances.Remove(originCell);
        }
    }
}
