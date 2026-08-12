using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// State của MỘT lần phát. Sinh mới mỗi lần PlayAsync, không tái dùng giữa 2 cutscene.
    public sealed class CutsceneRuntimeState
    {
        public int CurrentStepIndex;
        public int TotalSteps;
        public CutsceneEndReason EndReason;
        public bool NextStepRequested;

        // Nơi task cất asset đã load ở PrepareAsync. KHÔNG được cất vào field của SO.
        private readonly Dictionary<CutsceneTaskSO, Object> _preparedAssets = new();

        // Image do task Acquire được. Owner = task, dùng để Release đúng cái mình lấy ra.
        private readonly Dictionary<CutsceneTaskSO, Image> _imagesByTask = new();

        // Lookup ngang giữa các task: task A đăng ký id -> task B tra id để tween/cross-fade.
        private readonly Dictionary<string, Image> _imagesById = new();

        // VFX prefab do task spawn. Tách khỏi _imagesByTask vì View huỷ hẳn chứ không pool.
        private readonly Dictionary<CutsceneTaskSO, RectTransform> _vfxByTask = new();

        #region Public API
        public void Reset(int totalSteps)
        {
            CurrentStepIndex = 0;
            TotalSteps = totalSteps;
            EndReason = CutsceneEndReason.Completed;
            NextStepRequested = false;
            _preparedAssets.Clear();
            _imagesByTask.Clear();
            _imagesById.Clear();
            _vfxByTask.Clear();
        }

        public void SetPreparedAsset(CutsceneTaskSO task, Object asset)
        {
            if (task == null) return;

            _preparedAssets[task] = asset;
        }

        public T GetPreparedAsset<T>(CutsceneTaskSO task) where T : Object
        {
            if (task == null) return null;

            return _preparedAssets.TryGetValue(task, out Object asset) ? asset as T : null;
        }

        /// Ghi lại Image mà task vừa Acquire. Nếu <paramref name="id"/> có, đăng ký cả lookup ngang.
        public void RegisterImage(CutsceneTaskSO owner, string id, Image image)
        {
            if (owner == null || image == null) return;

            _imagesByTask[owner] = image;
            if (!string.IsNullOrEmpty(id)) _imagesById[id] = image;
        }

        public Image GetImageByTask(CutsceneTaskSO owner)
        {
            if (owner == null) return null;

            return _imagesByTask.TryGetValue(owner, out Image image) ? image : null;
        }

        public Image GetImageById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return _imagesById.TryGetValue(id, out Image image) ? image : null;
        }

        public void RegisterVfx(CutsceneTaskSO owner, RectTransform instance)
        {
            if (owner == null || instance == null) return;

            _vfxByTask[owner] = instance;
        }

        public RectTransform GetVfxByTask(CutsceneTaskSO owner)
        {
            if (owner == null) return null;

            return _vfxByTask.TryGetValue(owner, out RectTransform vfx) ? vfx : null;
        }

        public bool ConsumeNextStepRequest()
        {
            if (!NextStepRequested) return false;

            NextStepRequested = false;
            return true;
        }
        #endregion
    }
}
