using UnityEngine;

namespace Core.Module.Map
{
    /// <summary>
    /// Kho vật thể đặt được (ghế, cây, đất…) đã preload: tra ID → prefab.
    /// Đặt tên "Object" (không "Furniture") vì ObjectDatabaseSO chứa đủ loại vật.
    /// </summary>
    public interface IObjectCatalog
    {
        bool TryGet(int id, out GameObject prefab);
    }
}
