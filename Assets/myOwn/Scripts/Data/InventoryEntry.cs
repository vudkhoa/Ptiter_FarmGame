using UnityEngine;

namespace MyOwn.ServiceHarness
{
    public struct InventoryEntry{
        public string itemId;
        public int amount;

        public InventoryEntry(string id, int amt)
        {
            itemId = id;
            amount = amt;
        }
    }
}