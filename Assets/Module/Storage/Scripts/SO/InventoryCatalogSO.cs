using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Storage
{
    [CreateAssetMenu(
        fileName = "InventoryCatalog",
        menuName = "GDD/Inventory/Inventory Catalog")]
    public sealed class InventoryCatalogSO : ScriptableObject
    {
        [SerializeField] private List<InventoryItemDefinition> _items = new();

        public IReadOnlyList<InventoryItemDefinition> Items => _items;
    }
}
