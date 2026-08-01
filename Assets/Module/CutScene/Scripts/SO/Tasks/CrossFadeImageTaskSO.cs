using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Spawn 1 Image mới trong toSlot rồi fade in. Song song, nếu fromImageId trỏ tới 1 Image
    /// (do task khác đăng ký) thì fade nó xuống 0. Image cũ vẫn do owner của nó Release.
    /// </summary>
    [CreateAssetMenu(fileName = "CrossFadeImageTask", menuName = "GDD/Cutscene/Task/Cross Fade Image")]
    public sealed class CrossFadeImageTaskSO : CutsceneTaskSO
    {
        public AssetReferenceSprite nextSprite;

        [Tooltip("ID của Image sẵn có sẽ được fade xuống. Bỏ trống = chỉ fade in cái mới.")]
        public string fromImageId;

        public CutsceneImageSlot toSlot = CutsceneImageSlot.Foreground;

        [Tooltip("Optional. Đăng ký Image mới dưới id này để task sau tham chiếu.")]
        public string toImageId;

        [Min(0f)] public float duration = 0.6f;
        public Ease ease = Ease.InOutQuad;

        public override async UniTask PrepareAsync(CutsceneContext ctx, CancellationToken ct)
        {
            if (nextSprite == null || !nextSprite.RuntimeKeyIsValid()) return;

            try
            {
                var loaded = await ctx.Loader.LoadTrackerAsync(nextSprite);
                ctx.State.SetPreparedAsset(this, loaded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CrossFadeImageTaskSO] Load sprite thất bại ({name}): {e.Message}");
            }
        }

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var loaded = ctx.State.GetPreparedAsset<Sprite>(this);
            if (loaded == null) return UniTask.CompletedTask;

            var to = ctx.View?.AcquireImage(toSlot);
            if (to == null) return UniTask.CompletedTask;

            to.sprite = loaded;
            SetAlpha(to, 0f);
            ctx.State.RegisterImage(this, toImageId, to);

            var sequence = DOTween.Sequence().SetLink(to.gameObject);
            sequence.Join(to.DOFade(1f, duration).SetEase(ease));

            var from = ctx.State.GetImageById(fromImageId);
            if (from != null) sequence.Join(from.DOFade(0f, duration).SetEase(ease));

            return sequence.ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);
        }

        public override void Complete(CutsceneContext ctx)
        {
            var to = ctx.State.GetImageByTask(this);
            if (to != null) SetAlpha(to, 1f);

            var from = ctx.State.GetImageById(fromImageId);
            if (from != null) SetAlpha(from, 0f);
        }

        public override void Release(CutsceneContext ctx)
        {
            var to = ctx.State.GetImageByTask(this);
            if (to != null) ctx.View?.ReleaseImage(to);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
