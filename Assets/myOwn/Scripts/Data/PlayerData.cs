using System;
using System.Collections.Generic;
using Core.Module.Farm;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// POCO save model. [Serializable] để JsonUtility serialize. Sub-data nested cũng phải [Serializable].
    /// Bump SaveVersion khi đổi schema → handle migration ở PlayerDataSaveLoad.Load().
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int SaveVersion = 1;
        public long LastSaveUtcTicks;

        public int Coins = 1000;
        public List<InventoryEntry> Inventory = new();
        public List<FarmSlotSaveData> FarmSlots = new();
        public bool IsCheatDetected = false;

        public int GetItemCount(string itemId)
        {
            var entry = Inventory.Find(e => e.itemId == itemId);
            return entry.itemId != null ? entry.amount : 0;
        }

        public void AddItem(string itemId, int amount)
        {
            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == itemId)
                {
                    Inventory[i] = new InventoryEntry(itemId, Inventory[i].amount + amount);
                    return;
                }
            }
            Inventory.Add(new InventoryEntry(itemId, amount));
        }

        public bool RemoveItem(string itemId, int amount)
        {
            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == itemId)
                {
                    if (Inventory[i].amount < amount) return false;
                    Inventory[i] = new InventoryEntry(itemId, Inventory[i].amount - amount);
                    
                    if (Inventory[i].amount <= 0) Inventory.RemoveAt(i);
                    return true;    
                }
            }
            return false;
        }
    }
}
