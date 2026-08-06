using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    public sealed class MapObjectInstanceRegistry : IMapObjectInstanceRegistry
    {
        private readonly Dictionary<Vector3Int, GameObject> _instances = new();
        private readonly Dictionary<string, GameObject> _freeInstances = new();

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

        public bool TryGet(string instanceId, out GameObject instance)
        {
            if (!string.IsNullOrEmpty(instanceId)
                && _freeInstances.TryGetValue(instanceId, out instance)
                && instance != null) return true;

            instance = null;
            return false;
        }

        public void Register(string instanceId, GameObject instance)
        {
            if (string.IsNullOrEmpty(instanceId) || instance == null) return;
            _freeInstances[instanceId] = instance;
        }

        public bool RemoveAndDestroy(Vector3Int originCell)
        {
            if (!_instances.TryGetValue(originCell, out GameObject instance)) return false;
            _instances.Remove(originCell);
            if (instance != null) Object.Destroy(instance);
            return true;
        }

        public bool RemoveAndDestroy(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) ||
                !_freeInstances.TryGetValue(instanceId, out GameObject instance)) return false;
            _freeInstances.Remove(instanceId);
            if (instance != null) Object.Destroy(instance);
            return true;
        }

        public void MoveGridRegistration(Vector3Int oldOrigin, Vector3Int newOrigin)
        {
            if (!_instances.TryGetValue(oldOrigin, out GameObject instance)) return;
            _instances.Remove(oldOrigin);
            _instances[newOrigin] = instance;
        }

        public void ClearAndDestroy()
        {
            foreach (GameObject instance in _instances.Values)
            {
                if (instance != null) Object.Destroy(instance);
            }
            _instances.Clear();

            foreach (GameObject instance in _freeInstances.Values)
            {
                if (instance != null) Object.Destroy(instance);
            }
            _freeInstances.Clear();
        }
    }
}
