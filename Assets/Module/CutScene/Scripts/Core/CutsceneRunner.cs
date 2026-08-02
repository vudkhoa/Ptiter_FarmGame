using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Pure Logic, Full CutScene Algorithms. 
    /// </summary>
    public sealed class CutsceneRunner
    {
        // Prepare xong mới tới Run, step chạy tuần tự -> không có 2 chỗ đọc buffer cùng lúc.
        private readonly List<UniTask> _pending = new List<UniTask>(8);

        public UniTask PrepareAllAsync(CutsceneSO cutscene, CutsceneContext ctx, CancellationToken ct)
        {
            var steps = cutscene?.steps;
            if (steps == null) return UniTask.CompletedTask;

            _pending.Clear();
            for (int i = 0; i < steps.Count; i++)
            {
                var tasks = steps[i]?.tasks;
                if (tasks == null) continue;

                for (int j = 0; j < tasks.Count; j++)
                {
                    var task = tasks[j];
                    if (task != null) _pending.Add(PrepareOneAsync(task, ctx, ct));
                }
            }

            return _pending.Count == 0 ? UniTask.CompletedTask : UniTask.WhenAll(_pending);
        }

        /// <param name="onStepStarted">Service publish event ở đây - Runner không biết MessagePipe.</param>
        public async UniTask RunAsync(
            CutsceneSO cutscene,
            CutsceneContext ctx,
            CancellationToken ct,
            Action<int, CutsceneStepSO> onStepStarted = null)
        {
            var steps = cutscene?.steps;
            if (steps == null) return;

            for (int i = 0; i < steps.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var step = steps[i];
                if (step == null) continue;

                ctx.State.CurrentStepIndex = i;
                onStepStarted?.Invoke(i, step);

                await RunStepAsync(step, ctx, ct);
            }
        }

        private async UniTask RunStepAsync(CutsceneStepSO step, CutsceneContext ctx, CancellationToken ct)
        {
            var tasks = step.tasks;
            if (tasks == null || tasks.Count == 0) return;

            if (step.runMode == CutsceneRunMode.Sequential)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task != null) await RunOneAsync(task, ctx, ct);
                }
                return;
            }

            _pending.Clear();
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task != null) _pending.Add(RunOneAsync(task, ctx, ct));
            }

            if (_pending.Count > 0) await UniTask.WhenAll(_pending);
        }

        public void CompleteAll(CutsceneSO cutscene, CutsceneContext ctx)
            => ForEachTask(cutscene, ctx, static (task, c) => task.Complete(c), "Complete");

        public void ReleaseAll(CutsceneSO cutscene, CutsceneContext ctx)
            => ForEachTask(cutscene, ctx, static (task, c) => task.Release(c), "Release");

        private static async UniTask PrepareOneAsync(CutsceneTaskSO task, CutsceneContext ctx, CancellationToken ct)
        {
            try
            {
                await task.PrepareAsync(ctx, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                // Prepare hỏng -> task coi như vô hiệu, phần còn lại của cutscene vẫn chạy.
                Debug.LogError($"[CutsceneRunner] Prepare '{task.name}' lỗi: {e.Message}");
            }
        }

        private static async UniTask RunOneAsync(CutsceneTaskSO task, CutsceneContext ctx, CancellationToken ct)
        {
            if (task.startDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(task.startDelay), cancellationToken: ct);

            try
            {
                await task.RunAsync(ctx, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                Debug.LogError($"[CutsceneRunner] Run '{task.name}' lỗi: {e.Message}");
            }
        }

        // Complete/Release dùng chung 1 vòng duyệt; delegate static nên không cấp phát closure.
        private static void ForEachTask(
            CutsceneSO cutscene,
            CutsceneContext ctx,
            Action<CutsceneTaskSO, CutsceneContext> action,
            string phase)
        {
            var steps = cutscene?.steps;
            if (steps == null) return;

            for (int i = 0; i < steps.Count; i++)
            {
                var tasks = steps[i]?.tasks;
                if (tasks == null) continue;

                for (int j = 0; j < tasks.Count; j++)
                {
                    var task = tasks[j];
                    if (task == null) continue;

                    // 1 task lỗi không được chặn các task còn lại trả tài nguyên.
                    try { action(task, ctx); }
                    catch (Exception e) { Debug.LogError($"[CutsceneRunner] {phase} '{task.name}' lỗi: {e.Message}"); }
                }
            }
        }
    }
}
