using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "FadeScreenTask", menuName = "GDD/Cutscene/Task/Fade Screen")]
    public sealed class FadeScreenTaskSO : CutsceneTaskSO
    {
        [Range(0f, 1f)] public float targetAlpha = 1f;
        [Min(0f)] public float duration = 0.5f;
        public Ease ease = Ease.OutQuad;

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var group = ctx.View?.RootCanvasGroup;
            if (group == null) return UniTask.CompletedTask;

            // SetLink: tween tự chết khi object bị destroy (đổi scene giữa cutscene).
            return group.DOFade(targetAlpha, duration)
                        .SetEase(ease)
                        .SetLink(group.gameObject)
                        .ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);
        }

        public override void Complete(CutsceneContext ctx)
        {
            var group = ctx.View?.RootCanvasGroup;
            if (group != null) group.alpha = targetAlpha;
        }
    }
}
