using System;
using System.IO;
using UnityEngine;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Pure IO + JSON serialization. KHÔNG hold runtime state — chỉ PlayerDataHolder gọi.
    /// Atomic write (temp + rename) tránh corrupt nếu crash mid-write.
    /// </summary>
    public static class PlayerDataSaveLoad
    {
        private const string FILE_NAME = "playerdata.json";
        private const string TEMP_SUFFIX = ".tmp";

        private static string _filePath;
        private static string _tempPath;

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
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerDataSaveLoad] Load failed: {e.Message}. Returning null.");
                return null;
            }
        }

        /// <summary>Atomic save: write temp file → rename. Tránh corrupt nếu crash giữa chừng.</summary>
        public static void Save(PlayerData data)
        {
            if (data == null) return;
            try
            {
                var json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(TempPath, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(TempPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSaveLoad] Save failed: {e.Message}");
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
