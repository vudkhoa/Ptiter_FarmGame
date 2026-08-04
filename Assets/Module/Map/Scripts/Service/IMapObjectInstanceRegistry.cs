using UnityEngine;

namespace Core.Module.Map
{
    /// <summary>
    /// Connects logical map placements to their spawned scene instances.
    /// Gameplay modules can use this to resolve authored anchors on a placed object.
    /// </summary>
    public interface IMapObjectInstanceRegistry
    {
        void Register(Vector3Int originCell, GameObject instance);
        void Register(string instanceId, GameObject instance);
        bool TryGetAtOrigin(Vector3Int originCell, out GameObject instance);
        void Unregister(Vector3Int originCell);
        bool RemoveAndDestroy(Vector3Int originCell);
        bool RemoveAndDestroy(string instanceId);
        void ClearAndDestroy();
    }
}
