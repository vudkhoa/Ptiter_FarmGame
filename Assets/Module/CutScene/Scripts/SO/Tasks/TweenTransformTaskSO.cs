using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Cutscene
{
    /// <summary>Hiệu ứng Ken Burns: pan + zoom chậm trên 1 Image do task khác đăng ký.</summary>
    [CreateAssetMenu(fileName = "TweenTransformTask", menuName = "GDD/Cutscene/Task/Tween Transform")]
    public sealed class TweenTransformTaskSO : CutsceneTaskSO
    {
        [Tooltip("ID của Image cần tween (ShowImage / CrossFade task khác đăng ký).")]
        public string targetImageId;

        [Header("Position (anchored)")]
        public bool tweenPosition = true;
        public Vector2 fromPosition;
        public Vector2 toPosition;

        [Header("Scale")]
        public bool tweenScale = true;
        public Vector3 fromScale = Vector3.one;
        public Vector3 toScale = Vector3.one * 1.1f;

        [Header("Rotation (độ, quanh trục Z)")]
        public bool tweenRotation;
        public float fromRotationZ;
        public float toRotationZ;

        [Min(0f)] public float duration = 3f;
        public Ease ease = Ease.Linear;

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var image = ctx.State.GetImageById(targetImageId);
            if (image == null)
            {
                Debug.LogWarning($"[TweenTransformTaskSO] Không tìm thấy image id '{targetImageId}' ({name}).");
                return UniTask.CompletedTask;
            }

            var rect = image.rectTransform;
            ApplyStart(rect);

            var sequence = DOTween.Sequence().SetLink(rect.gameObject);
            if (tweenPosition) sequence.Join(rect.DOAnchorPos(toPosition, duration).SetEase(ease));
            if (tweenScale) sequence.Join(rect.DOScale(toScale, duration).SetEase(ease));
            if (tweenRotation)
                sequence.Join(rect.DOLocalRotate(new Vector3(0f, 0f, toRotationZ), duration, RotateMode.Fast).SetEase(ease));

            return sequence.ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);
        }

        public override void Complete(CutsceneContext ctx)
        {
            var image = ctx.State.GetImageById(targetImageId);
            if (image == null) return;

            var rect = image.rectTransform;
            if (tweenPosition) rect.anchoredPosition = toPosition;
            if (tweenScale) rect.localScale = toScale;
            if (tweenRotation) rect.localRotation = Quaternion.Euler(0f, 0f, toRotationZ);
        }

        private void ApplyStart(RectTransform rect)
        {
            if (tweenPosition) rect.anchoredPosition = fromPosition;
            if (tweenScale) rect.localScale = fromScale;
            if (tweenRotation) rect.localRotation = Quaternion.Euler(0f, 0f, fromRotationZ);
        }
    }
}
