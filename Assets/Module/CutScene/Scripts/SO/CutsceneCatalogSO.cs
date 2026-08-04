using System.Collections.Generic;
using BrunoMikoski.UIManager;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "CutsceneCatalog", menuName = "GDD/Cutscene/Cutscene Catalog")]
    public sealed class CutsceneCatalogSO : ScriptableObject
    {
        [Tooltip("Window dùng để chiếu mọi cutscene. Chọn CutsceneWindow trong UIWindowCollection.")]
        public UIWindowIndirectReference cutsceneWindow;

        public List<CutsceneSO> cutscenes = new List<CutsceneSO>();

        private Dictionary<string, CutsceneSO> _lookup;

        public CutsceneSO GetById(string cutsceneId)
        {
            if (string.IsNullOrWhiteSpace(cutsceneId)) return null;
            BuildLookup();
            return _lookup.TryGetValue(cutsceneId, out var cutscene) ? cutscene : null;
        }

        // Initialize lookup.
        private void BuildLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, CutsceneSO>(cutscenes.Count);
            for (int i = 0; i < cutscenes.Count; i++)
            {
                var cutscene = cutscenes[i];
                if (cutscene == null || string.IsNullOrWhiteSpace(cutscene.cutsceneId)) continue;
                _lookup[cutscene.cutsceneId] = cutscene;
            }
        }

        // Xoá cache khi rời Play Mode, tránh SO giữ state cũ giữa 2 lần chạy.
        private void OnDisable() => _lookup = null;
    }
}
