using System;

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

        // TODO: thêm game-specific fields (Currency, Inventory, Quests, ...).
    }
}
