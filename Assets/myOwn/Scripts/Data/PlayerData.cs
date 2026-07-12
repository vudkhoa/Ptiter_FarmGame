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
        public string PlayerId;
        public int SaveVersion = 1;
        public long LastSaveUtcTicks;

        public int Coins = 1000;
        public List<InventoryEntry> Inventory = new List<InventoryEntry>();
        public List<FarmSlotSaveData> FarmSlots = new List<FarmSlotSaveData>();

        // Flag raised if clock manipulation is detected to freeze production
        public bool IsCheatDetected = false;

        [NonSerialized]
        private Dictionary<string, int> _inventoryCache;

        private void RebuildInventoryCache()
        {
            if (_inventoryCache == null)
                _inventoryCache = new Dictionary<string, int>();
            else
                _inventoryCache.Clear();

            if (Inventory != null)
            {
                foreach (var entry in Inventory)
                {
                    if (entry.itemId != null)
                    {
                        _inventoryCache[entry.itemId] = entry.amount;
                    }
                }
            }
        }

        #region Inventory Helpers
        public int GetItemCount(string id)
        {
            if (_inventoryCache == null) RebuildInventoryCache();
            return _inventoryCache.TryGetValue(id, out var amt) ? amt : 0;
        }

        public void AddItem(string id, int amount)
        {
            if (Inventory == null) Inventory = new List<InventoryEntry>();
            if (_inventoryCache == null) RebuildInventoryCache();

            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == id)
                {
                    int newAmount = Inventory[i].amount + amount;
                    Inventory[i] = new InventoryEntry(id, newAmount);
                    _inventoryCache[id] = newAmount;
                    return;
                }
            }
            Inventory.Add(new InventoryEntry(id, amount));
            _inventoryCache[id] = amount;
        }

        public bool RemoveItem(string id, int amount)
        {
            if (Inventory == null) return false;
            if (_inventoryCache == null) RebuildInventoryCache();

            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].itemId == id)
                {
                    if (Inventory[i].amount < amount) return false;
                    int newAmount = Inventory[i].amount - amount;
                    if (newAmount <= 0)
                    {
                        Inventory.RemoveAt(i);
                        _inventoryCache.Remove(id);
                    }
                    else
                    {
                        Inventory[i] = new InventoryEntry(id, newAmount);
                        _inventoryCache[id] = newAmount;
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
