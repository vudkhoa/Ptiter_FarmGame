using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "WaitForInputTask", menuName = "GDD/Cutscene/Task/Wait For Input")]
    public sealed class WaitForInputTaskSO : CutsceneTaskSO
    {
        [Tooltip("0 = chờ vô hạn cho tới khi người chơi tap.")]
        [Min(0f)] public float timeoutSeconds;

        public override async UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var input = ctx.Input;
            var state = ctx.State;
            if (input == null) return;

            // Chờ nhả nút trước, tránh ăn luôn cú click vừa mở cutscene.
            await UniTask.WaitWhile(() => input.IsButtonHeld(0), cancellationToken: ct);

            var waitTap = UniTask.WaitUntil(
                () => input.IsButtonHeld(0) || state.ConsumeNextStepRequest(),
                cancellationToken: ct);

            if (timeoutSeconds <= 0f)
            {
                await waitTap;
                return;
            }

            await UniTask.WhenAny(
                waitTap,
                UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken: ct));
        }
    }
}
