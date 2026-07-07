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
        public List<InventoryEntry> Inventory = new List<InventoryEntry>();
        public List<FarmSlotSaveData> FarmSlots = new List<FarmSlotSaveData>();

        // Flag raised if clock manipulation is detected to freeze production
        public bool IsCheatDetected = false;

        #region Inventory Helpers
        public int GetItemCount(string id)
        {
            if (Inventory == null) return 0;
            var entry = Inventory.Find(e => e.itemId == id);
            return entry.itemId != null ? entry.amount : 0;
        }

        public void AddItem(string id, int amount)
        {
            if (Inventory == null) Inventory = new List<InventoryEntry>();
            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == id)
                {
                    Inventory[i] = new InventoryEntry(id, Inventory[i].amount + amount);
                    return;
                }
            }
            Inventory.Add(new InventoryEntry(id, amount));
        }

        public bool RemoveItem(string id, int amount)
        {
            if (Inventory == null) return false;
            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == id)
                {
                    if (Inventory[i].amount < amount) return false;
                    Inventory[i] = new InventoryEntry(id, Inventory[i].amount - amount);
                    if (Inventory[i].amount <= 0) Inventory.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
