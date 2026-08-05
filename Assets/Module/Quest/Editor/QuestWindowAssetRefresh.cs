#if UNITY_EDITOR
using UnityEditor;

namespace Core.Module.Quest.Editor
{
    /// <summary>
    /// Refreshes the Quest prefab reference after external prefab restoration.
    /// This does not rebuild or modify the Quest UI hierarchy.
    /// </summary>
    internal static class QuestWindowAssetRefresh
    {
        private const string SessionKey = "QuestWindow.AssetRefresh20260802";
        private const string PrefabPath = "Assets/Module/Quest/UI/QuestWindow.prefab";
        private const string WindowAssetPath =
            "Assets/Module/UI System/package/SO/Windows/Items/QuestWindow.asset";

        [InitializeOnLoadMethod]
        private static void RefreshOnce()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += RefreshAssets;
        }

        private static void RefreshAssets()
        {
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(WindowAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
