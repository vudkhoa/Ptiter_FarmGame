using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Base cho mọi task. LUẬT: field chỉ được ĐỌC, cấm lưu state runtime vào SO.
    /// </summary>
    public abstract class CutsceneTaskSO : ScriptableObject
    {
        public string taskName;
        [Min(0f)] public float startDelay;

        /// <summary>Load asset trước khi cutscene chạy. Mặc định no-op.</summary>
        public virtual UniTask PrepareAsync(CutsceneContext ctx, CancellationToken ct)
            => UniTask.CompletedTask;

        public abstract UniTask RunAsync(CutsceneContext ctx, CancellationToken ct);

        /// <summary>Snap về trạng thái cuối khi bị skip. Mặc định no-op.</summary>
        public virtual void Complete(CutsceneContext ctx) { }

        public virtual void Release(CutsceneContext ctx) { }
    }
}
