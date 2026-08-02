using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    [CreateAssetMenu(fileName = "ShowImageTask", menuName = "GDD/Cutscene/Task/Show Image")]
    public sealed class ShowImageTaskSO : CutsceneTaskSO
    {
        public AssetReferenceSprite sprite;
        public CutsceneImageSlot slot = CutsceneImageSlot.Background;

        [Tooltip("Optional. Đăng ký Image dưới id này để Tween / CrossFade task khác tham chiếu.")]
        public string imageId;

        [Min(0f)] public float fadeDuration = 0.5f;
        public Ease ease = Ease.OutQuad;

        public override async UniTask PrepareAsync(CutsceneContext ctx, CancellationToken ct)
        {
            if (sprite == null || !sprite.RuntimeKeyIsValid()) return;

            try
            {
                var loaded = await ctx.Loader.LoadTrackerAsync(sprite);
                ctx.State.SetPreparedAsset(this, loaded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShowImageTaskSO] Load sprite thất bại ({name}): {e.Message}");
            }
        }

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var loaded = ctx.State.GetPreparedAsset<Sprite>(this);
            if (loaded == null) return UniTask.CompletedTask;

            var image = ctx.View?.AcquireImage(slot);
            if (image == null) return UniTask.CompletedTask;

            image.sprite = loaded;
            ctx.State.RegisterImage(this, imageId, image);

            return image.DOFade(1f, fadeDuration)
                        .SetEase(ease)
                        .SetLink(image.gameObject)
                        .ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);
        }

        public override void Complete(CutsceneContext ctx)
        {
            var image = ctx.State.GetImageByTask(this);
            if (image == null) return;

            var color = image.color;
            color.a = 1f;
            image.color = color;
        }

        public override void Release(CutsceneContext ctx)
        {
            var image = ctx.State.GetImageByTask(this);
            if (image != null) ctx.View?.ReleaseImage(image);
        }
    }
}
