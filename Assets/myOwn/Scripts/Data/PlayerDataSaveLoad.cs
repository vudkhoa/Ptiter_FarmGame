using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Pure IO + JSON serialization. KHÔNG hold runtime state — chỉ PlayerDataHolder gọi.
    /// Atomic write (temp + rename) tránh corrupt nếu crash mid-write.
    /// </summary>
    public static class PlayerDataSaveLoad
    {
        private const int CURRENT_SAVE_VERSION = 3;
        private const string FILE_NAME = "playerdata.json";
        private const string TEMP_SUFFIX = ".tmp";

        private static string _filePath;
        private static string _tempPath;
        private static readonly object SaveGate = new object();
        private static long _nextSaveRevision;
        private static long _latestCommittedRevision;

        private static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    // Lần truy xuất đầu tiên sẽ luôn từ Main Thread (khi Load được gọi lúc bắt đầu game)
                    _filePath = Path.Combine(Application.persistentDataPath, FILE_NAME);
                }
                return _filePath;
            }
        }

        private static string TempPath
        {
            get
            {
                if (string.IsNullOrEmpty(_tempPath))
                {
                    _tempPath = FilePath + TEMP_SUFFIX;
                }
                return _tempPath;
            }
        }

        /// <summary>Returns null nếu file không tồn tại hoặc parse fail.</summary>
        public static PlayerData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json)) return null;
                var data = JsonUtility.FromJson<PlayerData>(json);
                NormalizeLoadedData(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerDataSaveLoad] Load failed: {e.Message}. Returning null.");
                return null;
            }
        }

        private static void NormalizeLoadedData(PlayerData data)
        {
            if (data == null) return;

            // JsonUtility leaves fields added in a newer schema null when reading
            // an older save. Keep existing progress and only initialize missing data.
            data.Inventory ??= new System.Collections.Generic.List<InventoryEntry>();
            data.FarmSlots ??=
                new System.Collections.Generic.List<Core.Module.Farm.FarmSlotSaveData>();
            data.MapPlacements ??=
                new System.Collections.Generic.List<Core.Module.Map.MapPlacementSaveData>();
            data.PendingQuestRewards ??=
                new System.Collections.Generic.List<
                    Core.Module.Quest.PendingQuestRewardSaveData>();
            data.GrantedQuestRewardTransactions ??=
                new System.Collections.Generic.List<string>();

            if (data.SaveVersion < 3)
            {
                bool hasBonsai = data.Inventory.Exists(
                    entry => entry.itemId == "bonsai");
                if (!hasBonsai)
                    data.Inventory.Add(new InventoryEntry("bonsai", 6));
            }

            if (data.SaveVersion < CURRENT_SAVE_VERSION)
                data.SaveVersion = CURRENT_SAVE_VERSION;
        }

        /// <summary>Atomic save: write temp file → rename. Tránh corrupt nếu crash giữa chừng.</summary>
        public static bool Save(PlayerData data)
        {
            if (data == null) return false;
            return SaveJson(
                JsonUtility.ToJson(data, prettyPrint: false),
                ReserveSaveRevision());
        }

        public static long ReserveSaveRevision()
        {
            return Interlocked.Increment(ref _nextSaveRevision);
        }

        public static bool SaveJson(string json, long revision)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                lock (SaveGate)
                {
                    // A canceled throttled writer may finish after an immediate save.
                    // Never let that older snapshot overwrite the newer transaction.
                    if (revision < _latestCommittedRevision)
                        return true;
                    File.WriteAllText(TempPath, json);
                    if (File.Exists(FilePath)) File.Delete(FilePath);
                    File.Move(TempPath, FilePath);
                    _latestCommittedRevision = revision;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSaveLoad] Save failed: {e.Message}");
                return false;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSaveLoad] Delete failed: {e.Message}");
            }
        }

        public static bool SaveExists() => File.Exists(FilePath);
    }
}
