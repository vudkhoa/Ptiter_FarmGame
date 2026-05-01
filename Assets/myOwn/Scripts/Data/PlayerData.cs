using System;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// POCO save model. Greenfield template — dev điền field game-specific.
    /// Phải [Serializable] để JsonUtility serialize được.
    /// </summary>
    /// <remarks>
    /// Convention:
    /// - Tất cả field public hoặc [SerializeField] private để JsonUtility nhìn thấy.
    /// - Sub-data nested phải cũng [Serializable].
    /// - Bump SaveVersion khi đổi schema → migration logic ở PlayerDataSaveLoad.Load().
    /// </remarks>
    [Serializable]
    public class PlayerData
    {
        public int SaveVersion = 1;
        public long LastSaveUtcTicks;

        // TODO: Add your game-specific fields, ví dụ:
        // public CurrencyData Currency = new();
        // public InventoryData Inventory = new();
        // public List<QuestProgress> Quests = new();
    }
}
