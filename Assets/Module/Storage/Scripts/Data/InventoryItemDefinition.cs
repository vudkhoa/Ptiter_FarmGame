using System;
using UnityEngine;

namespace Core.Module.Storage
{
    [Serializable]
    public sealed class InventoryItemDefinition
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public InventoryCategory category;
        [Min(0)] public int sellPrice;
        public Sprite icon;
        public Sprite preview;
    }
}
