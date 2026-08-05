using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "WaitTask", menuName = "GDD/Cutscene/Task/Wait")]
    public sealed class WaitTaskSO : CutsceneTaskSO
    {
        [Min(0f)] public float seconds = 1f;

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
            => UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
    }
}
