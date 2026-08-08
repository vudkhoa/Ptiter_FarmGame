using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Chuyển động nền lặp vô hạn cho các lớp tĩnh: mây trôi, lúa đung đưa, người thở,
    /// hào quang xoay. Trả về NGAY để cutscene chạy tiếp sang frame khác — chuyển động
    /// sống tới lúc ảnh bị thu hồi khi đóng popup. Muốn giữ khung lại cho người chơi kịp
    /// nhìn thì nối thêm một WaitTaskSO phía sau trong step.
    ///
    /// Khác BreathLoopTaskSO ở hai điểm: chỉ đụng transform (không đụng alpha), và mỗi lớp
    /// có chu kỳ + độ trễ riêng — lúa với mây mà đập chung một nhịp thì lộ ngay là máy chạy.
    ///
    /// Skip không đụng gì tới nhịp: đây là hiệu ứng nền, cứ để nó chạy tiếp.
    /// </summary>
    [CreateAssetMenu(fileName = "LoopMotionTask", menuName = "GDD/Cutscene/Task/Loop Motion")]
    public sealed class LoopMotionTaskSO : CutsceneTaskSO
    {
        private const float AmplitudeEpsilon = 0.0001f;
        private const float MinSpinSpeed = 0.01f;

        /// <summary>Một lớp chịu chuyển động. Trỏ tới Image bằng id do ShowImage task đăng ký.</summary>
        [Serializable]
        public sealed class MotionTargetRef
        {
            [Tooltip("imageId mà ShowImageTaskSO đã đăng ký cho lớp này.")]
            public string imageId;

            [Tooltip("Tâm xoay/phóng, toạ độ chuẩn hoá trong rect. (0.5,0.5) là giữa ảnh, " +
                     "(0.5,0) là mép dưới — dùng cho lúa đung đưa hay người thở từ gót chân.")]
            public Vector2 pivot = new Vector2(0.5f, 0.5f);

            [Header("Biên độ, dao động hai chiều quanh trạng thái gốc")]
            [Tooltip("Nghiêng tối đa quanh pivot, đơn vị độ.")]
            public float rotationDegrees;

            [Tooltip("Trượt tối đa tính bằng pixel, theo trục local của rect nên lớp nghiêng vẫn đi đúng phương ngang của khung.")]
            public Vector2 positionOffset;

            [Tooltip("Phình tối đa. 0.02 = 2%.")]
            public float scaleAmount;

            [Header("Nhịp")]
            [Tooltip("Trọn một vòng đi và về.")]
            [Min(0.1f)] public float cycleSeconds = 3f;

            [Tooltip("Đứng yên bao lâu rồi mới vào nhịp, tính theo phần của chu kỳ. Dùng để các lớp lệch nhau.")]
            [Range(0f, 1f)] public float phase;

            public Ease ease = Ease.InOutSine;

            [Header("Quay đều")]
            [Tooltip("Khác 0 thì lớp quay tròn liên tục với tốc độ này (độ/giây) và bỏ qua phần dao động ở trên.")]
            public float spinDegreesPerSecond;
        }

        public MotionTargetRef[] targets = Array.Empty<MotionTargetRef>();

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            MotionTargetRef[] refs = targets;
            if (refs == null) return UniTask.CompletedTask;

            for (int i = 0; i < refs.Length; i++)
            {
                MotionTargetRef target = refs[i];
                if (target == null) continue;

                Image image = ctx.State.GetImageById(target.imageId);
                if (image != null) Play(image.rectTransform, target);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Đóng popup: phải tự giết tween. SetLink chỉ cứu khi GameObject bị Destroy, mà View
        /// pool ảnh lại chứ không huỷ. Transform thì ResetImageState lo giùm lúc lấy ảnh ra khỏi pool.
        /// </summary>
        public override void Release(CutsceneContext ctx)
        {
            MotionTargetRef[] refs = targets;
            if (refs == null) return;

            for (int i = 0; i < refs.Length; i++)
            {
                MotionTargetRef target = refs[i];
                if (target == null) continue;

                Image image = ctx.State.GetImageById(target.imageId);
                if (image != null) DOTween.Kill(image.rectTransform);
            }
        }

        private static void Play(RectTransform rect, MotionTargetRef target)
        {
            // Đổi pivot trước khi chốt giá trị gốc: xoay và phóng đều bám pivot.
            SetPivotKeepPlace(rect, target.pivot);

            // Chạy lại cutscene: giết vòng cũ, tránh hai tween cùng ghi transform.
            // Target là RectTransform chứ không phải GameObject, để DOTween.Kill(gameObject)
            // bên BreathLoopTaskSO không giết nhầm khi một lớp vừa xoay vừa thở.
            DOTween.Kill(rect);

            float baseRotation = rect.localEulerAngles.z;

            if (Mathf.Abs(target.spinDegreesPerSecond) > MinSpinSpeed)
            {
                Spin(rect, baseRotation, target.spinDegreesPerSecond);
                return;
            }

            float tilt = target.rotationDegrees;
            Vector2 slide = target.positionOffset;
            float swell = target.scaleAmount;

            // Chỉ ghi đúng thuộc tính thật sự có biên độ. Đa số lớp chỉ dùng một thứ (heo scale,
            // mây trượt, lúa nghiêng) — ghi cả ba mỗi frame là bắt Canvas dirty vô ích.
            bool tilts = Mathf.Abs(tilt) > AmplitudeEpsilon;
            bool slides = slide.sqrMagnitude > AmplitudeEpsilon * AmplitudeEpsilon;
            bool swells = Mathf.Abs(swell) > AmplitudeEpsilon;
            if (!tilts && !slides && !swells) return;

            Quaternion baseOrientation = rect.localRotation;
            Vector2 basePosition = rect.anchoredPosition;
            Vector3 baseScale = rect.localScale;
            float wave = 0f;

            void Apply(float value)
            {
                // Gán ngược cho getter bên dưới: mỗi đoạn trong Sequence đọc lại biến này
                // làm giá trị khởi đầu của nó, nên đây không phải dòng thừa.
                wave = value;

                Quaternion orientation = tilts
                    ? Quaternion.Euler(0f, 0f, baseRotation + value * tilt)
                    : baseOrientation;

                if (tilts) rect.localRotation = orientation;
                if (swells) rect.localScale = baseScale * (1f + value * swell);
                if (slides) rect.anchoredPosition = basePosition + (Vector2)(orientation * (value * slide));
            }

            float quarter = target.cycleSeconds * 0.25f;

            // Chia làm ba đoạn để nhịp KHỞI HÀNH TỪ ĐÚNG TRẠNG THÁI GỐC. Tween yoyo -1 -> 1 gọn
            // hơn nhưng nó nhảy phắt xuống biên dưới ngay frame đầu, nhìn ra giật một phát.
            // Hai đoạn rìa cố tình dùng OutSine/InSine để vận tốc nối liền với đoạn giữa —
            // để cả ba đoạn cùng InOutSine thì mỗi lần đi ngang điểm gốc lớp lại khựng.
            DOTween.Sequence()
                   .Append(DOTween.To(() => wave, Apply, 1f, quarter).SetEase(Ease.OutSine))
                   .Append(DOTween.To(() => wave, Apply, -1f, quarter * 2f).SetEase(target.ease))
                   .Append(DOTween.To(() => wave, Apply, 0f, quarter).SetEase(Ease.InSine))
                   .SetLoops(-1)
                   // false = delay thật, không phải interval chèn đầu Sequence — chèn interval thì
                   // vòng lặp nào cũng đứng hình chừng đó giây.
                   .SetDelay(target.phase * target.cycleSeconds, false)
                   .SetTarget(rect)
                   .SetLink(rect.gameObject);
        }

        private static void Spin(RectTransform rect, float baseRotation, float degreesPerSecond)
        {
            float sweep = degreesPerSecond > 0f ? 360f : -360f;
            float angle = 0f;

            DOTween.To(() => angle, value =>
                   {
                       angle = value;
                       rect.localRotation = Quaternion.Euler(0f, 0f, baseRotation + value);
                   }, sweep, 360f / Mathf.Abs(degreesPerSecond))
                   .SetEase(Ease.Linear)
                   .SetLoops(-1, LoopType.Restart)
                   .SetTarget(rect)
                   .SetLink(rect.gameObject);
        }

        /// <summary>Dời pivot mà ảnh không nhảy chỗ: bù đúng phần rect trượt đi vào anchoredPosition.</summary>
        private static void SetPivotKeepPlace(RectTransform rect, Vector2 pivot)
        {
            Vector2 delta = pivot - rect.pivot;
            if (delta.sqrMagnitude < AmplitudeEpsilon * AmplitudeEpsilon) return;

            Vector2 size = rect.rect.size;
            Vector3 scale = rect.localScale;
            var shift = new Vector2(delta.x * size.x * scale.x, delta.y * size.y * scale.y);

            rect.pivot = pivot;
            rect.anchoredPosition += (Vector2)(rect.localRotation * shift);
        }
    }
}
