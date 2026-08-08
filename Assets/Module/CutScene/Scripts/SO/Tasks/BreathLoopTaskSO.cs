using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Nhịp thở nền cho một shot: kéo nhiều Image bằng CHUNG một đường cong, loop vô hạn.
    /// Task trả về NGAY để cutscene chạy tiếp sang frame khác — nhịp vẫn thở nền cho tới khi
    /// ảnh được thu hồi lúc đóng popup. Muốn giữ khung lại cho người chơi kịp nhìn thì nối
    /// thêm một WaitTaskSO phía sau trong step.
    ///
    /// Shot 3 cần quầng lò, viền vai thợ và sắc ám toàn khung đập cùng lúc, nên tất cả phải
    /// nằm trên một Sequence duy nhất chứ không phải mỗi lớp một tween.
    ///
    /// Skip không đụng gì tới nhịp: đây là hiệu ứng nền, cứ để nó chạy tiếp.
    /// </summary>
    [CreateAssetMenu(fileName = "BreathLoopTask", menuName = "GDD/Cutscene/Task/Breath Loop")]
    public sealed class BreathLoopTaskSO : CutsceneTaskSO
    {
        /// <summary>Một lớp chịu tác động của nhịp. Trỏ tới Image bằng id do ShowImage task đăng ký.</summary>
        [Serializable]
        public sealed class BreathTargetRef
        {
            [Tooltip("imageId mà ShowImageTaskSO đã đăng ký cho lớp này.")]
            public string imageId;

            [Tooltip("Alpha ở ĐÁY nhịp.")]
            [Range(0f, 1f)] public float restAlpha;

            [Tooltip("Alpha ở ĐỈNH nhịp.")]
            [Range(0f, 1f)] public float peakAlpha = 1f;

            [Tooltip("Phình nhẹ theo nhịp.")]
            public bool swell;
        }

        public BreathTargetRef[] targets = Array.Empty<BreathTargetRef>();

        [Header("Nhịp")]
        [Tooltip("Độ dài một hơi thở. Kịch bản Shot 3 chốt 1.4 giây.")]
        [Min(0.1f)] public float cycleSeconds = 1.4f;

        [Tooltip("Phần chu kỳ dành cho lúc bùng lên. Càng nhỏ càng dốc; 0.5 thành nhấp nháy đều.")]
        [Range(0.05f, 0.9f)] public float riseRatio = 0.22f;

        [Tooltip("Phồng lên: nhanh, dốc.")]
        public Ease riseEase = Ease.OutQuad;

        [Tooltip("Lịm xuống: chậm, dài. Phải khác riseEase, không thì mất cảm giác thở.")]
        public Ease fallEase = Ease.InOutSine;

        [Range(0f, 0.2f)] public float swellScale = 0.028f;

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            BreathTargetRef[] refs = targets;
            int count = refs != null ? refs.Length : 0;
            if (count == 0) return UniTask.CompletedTask;

            // State chỉ sống trong closure của tween — SO không giữ gì, đúng luật CutsceneTaskSO.
            var images = new Image[count];
            var baseScales = new Vector3[count];
            var startAlphas = new float[count];
            Image anchor = null;

            for (int i = 0; i < count; i++)
            {
                BreathTargetRef target = refs[i];
                if (target == null) continue;

                Image image = ctx.State.GetImageById(target.imageId);
                if (image == null) continue;

                images[i] = image;
                baseScales[i] = image.rectTransform.localScale;
                // Ghi lại alpha ShowImage vừa fade lên, để lịm dần xuống đáy nhịp thay vì cắt phụt.
                startAlphas[i] = image.canvasRenderer.GetAlpha();
                anchor ??= image;
            }

            if (anchor == null) return UniTask.CompletedTask;

            // Chạy lại cutscene: giết nhịp cũ trước, tránh hai Sequence cùng ghi alpha.
            DOTween.Kill(anchor.gameObject);

            float rise = cycleSeconds * riseRatio;
            float fall = cycleSeconds - rise;
            float swell = swellScale;
            float breath = 0f;
            float settle = 0f;

            void ApplyBreath(float value)
            {
                // Gán ngược cho getter bên dưới: đoạn sau trong Sequence đọc lại biến này
                // làm giá trị khởi đầu của nó, nên đây không phải dòng thừa.
                breath = value;

                for (int i = 0; i < count; i++)
                {
                    Image image = images[i];
                    if (image == null) continue;

                    BreathTargetRef target = refs[i];

                    // SetAlpha đi thẳng vào CanvasRenderer nên không bắt Canvas dựng lại mesh mỗi frame.
                    // Lerp giữa sàn và đỉnh — không nhân thẳng value, để lớp không tắt hẳn ở đáy nhịp.
                    image.canvasRenderer.SetAlpha(Mathf.Lerp(target.restAlpha, target.peakAlpha, value));

                    if (target.swell)
                        image.rectTransform.localScale = baseScales[i] * (1f + value * swell);
                }
            }

            void ApplySettle(float value)
            {
                settle = value;

                for (int i = 0; i < count; i++)
                {
                    Image image = images[i];
                    if (image == null) continue;

                    image.canvasRenderer.SetAlpha(Mathf.Lerp(startAlphas[i], refs[i].restAlpha, value));
                }
            }

            // Vào nhịp: ảnh đang sáng hết cỡ sau cú fade của ShowImage, mà đáy nhịp thường tối hơn
            // nhiều — nhảy thẳng xuống là thấy giật ngay lúc frame vừa hiện. Cho nó lịm xuống đúng
            // bằng một hơi thở ra rồi nhịp mới bắt đầu.
            DOTween.To(() => settle, ApplySettle, 1f, fall)
                   .SetEase(fallEase)
                   .SetTarget(anchor.gameObject)
                   .SetLink(anchor.gameObject);

            DOTween.Sequence()
                   .Append(DOTween.To(() => breath, ApplyBreath, 1f, rise).SetEase(riseEase))
                   .Append(DOTween.To(() => breath, ApplyBreath, 0f, fall).SetEase(fallEase))
                   .SetLoops(-1)
                   // false = delay thật, không phải interval chèn đầu Sequence — chèn interval thì
                   // vòng lặp nào cũng đứng hình chừng đó giây.
                   .SetDelay(fall, false)
                   .SetTarget(anchor.gameObject)
                   .SetLink(anchor.gameObject);

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Đóng popup / thu hồi ảnh: phải tự giết tween.
        /// SetLink chỉ cứu khi GameObject bị Destroy, mà View pool ảnh lại chứ không huỷ —
        /// không giết ở đây thì nhịp cũ còn ghi alpha lên ảnh đã được tái sử dụng.
        /// </summary>
        public override void Release(CutsceneContext ctx)
        {
            BreathTargetRef[] refs = targets;
            if (refs == null) return;

            for (int i = 0; i < refs.Length; i++)
            {
                BreathTargetRef target = refs[i];
                if (target == null) continue;

                Image image = ctx.State.GetImageById(target.imageId);
                if (image == null) continue;

                DOTween.Kill(image.gameObject);

                // Trả alpha CanvasRenderer về 1, KHÔNG phải 0. ResetImageState lúc lấy ảnh từ pool
                // chỉ reset color.a, không đụng tới CanvasRenderer — để 0 thì lần tái sử dụng sau
                // ảnh sẽ tàng hình dù đã fade color.a lên 1.
                image.canvasRenderer.SetAlpha(1f);
            }
        }
    }
}
