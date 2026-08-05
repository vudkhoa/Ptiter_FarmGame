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
        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

        public AssetReferenceSprite sprite;
        public CutsceneImageSlot slot = CutsceneImageSlot.Background;

        [Tooltip("Optional. Đăng ký Image dưới id này để Tween / CrossFade task khác tham chiếu.")]
        public string imageId;

        [Min(0f)] public float fadeDuration = 0.5f;
        public Ease ease = Ease.OutQuad;

        [Header("Layout lúc spawn")]
        [Tooltip("Tắt = giữ nguyên rect của template trong prefab (ảnh nền full-screen).")]
        public bool overrideLayout;

        public Vector2 anchoredPosition;
        public Vector2 size = new Vector2(440f, 300f);

        [Tooltip("Góc nghiêng quanh trục Z, đơn vị độ.")]
        public float rotationZ;

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
            var image = Spawn(ctx);
            if (image == null) return UniTask.CompletedTask;

            return image.DOFade(1f, fadeDuration)
                        .SetEase(ease)
                        .SetLink(image.gameObject)
                        .ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);
        }

        public override void Complete(CutsceneContext ctx)
        {
            var image = ctx.State.GetImageByTask(this) ?? Spawn(ctx);
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

        private Image Spawn(CutsceneContext ctx)
        {
            var loaded = ctx.State.GetPreparedAsset<Sprite>(this);
            if (loaded == null) return null;

            var image = ctx.View?.AcquireImage(slot);
            if (image == null) return null;

            image.sprite = loaded;
            ApplyLayout(image.rectTransform);
            ctx.State.RegisterImage(this, imageId, image);
            return image;
        }

        private void ApplyLayout(RectTransform rect)
        {
            if (!overrideLayout) return;
            rect.anchorMin = CenterAnchor;
            rect.anchorMax = CenterAnchor;
            rect.pivot = CenterAnchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }
}
