#if UNITY_EDITOR
using MyOwn.ServiceHarness;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest.Editor
{
    internal sealed class QuestUIScrollMigration : AssetPostprocessor
    {
        private const string SessionKey = "QuestUI.VerticalScrollMigrationV1";
        private const string PrefabPath = "Assets/Module/Quest/UI/QuestWindow.prefab";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (!didDomainReload || SessionState.GetBool(SessionKey, false))
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null &&
                prefab.GetComponentInChildren<ScrollRect>(true) != null)
                return;

            SessionState.SetBool(SessionKey, true);
            QuestProjectSetup.Rebuild();
        }
    }
}
#endif
