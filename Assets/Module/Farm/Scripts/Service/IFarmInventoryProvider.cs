namespace Core.Module.Farm
{
    public interface IFarmInventoryProvider
    {
        int Coins { get; set; }
        bool IsCheatDetected { get; set; }
        long LastSaveUtcTicks { get; }

        int GetItemCount(string itemId);
        void AddItem(string itemId, int amount);
        bool RemoveItem(string itemId, int amount);
        void Save();
    }
}
