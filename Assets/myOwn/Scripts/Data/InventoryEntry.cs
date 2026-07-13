using System;
using UnityEngine;

namespace MyOwn.ServiceHarness
{
    [Serializable]
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