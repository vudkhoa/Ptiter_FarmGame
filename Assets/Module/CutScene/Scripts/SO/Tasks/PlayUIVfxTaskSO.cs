using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Spawn một prefab VFX vào slot ảnh, cho thứ mà Image tĩnh không diễn được: hào quang
    /// quay, tia sáng loé, đốm sáng bay quanh một lớp khác.
    ///
    /// Lớp trên/dưới theo ĐÚNG luật của ShowImageTaskSO - ai spawn trước nằm dưới. Muốn quầng
    /// sáng nằm sau tấm giấy thì xếp task này lên trước task vẽ tấm giấy trong cùng step.
    ///
    /// Module cố tình KHÔNG tham chiếu UIParticle: prefab tự lo phần render trong Canvas, task
    /// chỉ đặt chỗ và canh giờ. Nhờ vậy đổi sang loại VFX khác không phải đụng code, và
    /// asmdef CutScene không chết theo nếu package VFX chưa resolve.
    ///
    /// Không có field scale: UIParticle tự lái localScale của chính nó (AutoScalingMode.Transform),
    /// ghi vào đây chỉ bị nó ghi đè lại. Chỉnh to nhỏ bằng Scale3D trong prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayUIVfxTask", menuName = "GDD/Cutscene/Task/Play UI Vfx")]
    public sealed class PlayUIVfxTaskSO : CutsceneTaskSO
    {
        private const float MinSpinSpeed = 0.01f;

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

        public AssetReferenceGameObject prefab;
        public CutsceneImageSlot slot = CutsceneImageSlot.Foreground;

        [Header("Layout lúc spawn")]
        [Tooltip("Tắt = giữ nguyên rect gốc của prefab.")]
        public bool overrideLayout = true;

        public Vector2 anchoredPosition;

        [Tooltip("Góc nghiêng quanh trục Z, đơn vị độ. Chỉnh cho khớp độ nghiêng của lớp mà VFX chạy sau lưng.")]
        public float rotationZ;

        [Tooltip("0 = spawn xong nhả quyền điều khiển ngay, VFX cứ chạy tới lúc cutscene đóng. " +
                 "Khác 0 = giữ step lại chừng đó giây rồi mới sang step sau.")]
        [Min(0f)] public float holdSeconds;

        [Header("Xoay vòng quanh tâm")]
        [Tooltip("Khác 0 = quầng sáng quay đều quanh trục Z với tốc độ này (độ/giây), quay tới lúc cutscene đóng. " +
                 "Chỉ xoay được, KHÔNG punch scale ở đây: UIParticle giữ quyền ghi localScale (AutoScalingMode.Transform) " +
                 "và bù ngược cả scale của parent, nên mọi tween scale từ ngoài đều bị nó ghi đè lại ngay frame sau.")]
        public float spinDegreesPerSecond;

        public override async UniTask PrepareAsync(CutsceneContext ctx, CancellationToken ct)
        {
            if (prefab == null || !prefab.RuntimeKeyIsValid()) return;

            try
            {
                var loaded = await ctx.Loader.LoadTrackerAsync(prefab);
                ctx.State.SetPreparedAsset(this, loaded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayUIVfxTaskSO] Load prefab thất bại ({name}): {e.Message}");
            }
        }

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            var instance = Spawn(ctx);
            if (instance == null || holdSeconds <= 0f) return UniTask.CompletedTask;

            return UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: ct);
        }

        /// <summary>
        /// Cố tình để trống. Complete chỉ chạy khi cutscene bị skip/huỷ, mà ngay sau đó là
        /// Hide window + ReleaseAll - spawn quầng sáng ra để huỷ liền là làm việc thừa.
        /// Particle cũng không có "khung cuối" để snap tới như một ảnh đang fade.
        /// </summary>
        public override void Complete(CutsceneContext ctx) { }

        public override void Release(CutsceneContext ctx)
        {
            var instance = ctx.State.GetVfxByTask(this);
            if (instance == null) return;

            // Giết vòng xoay trước khi View Destroy: SetLink dọn được, nhưng nó chờ tới frame sau
            // mới thấy GameObject chết, còn đây thì tween buông rect ngay tại chỗ.
            DOTween.Kill(instance);
            ctx.View?.ReleaseVfx(instance);
        }

        private RectTransform Spawn(CutsceneContext ctx)
        {
            var loaded = ctx.State.GetPreparedAsset<GameObject>(this);
            if (loaded == null) return null;

            var instance = ctx.View?.AcquireVfx(slot, loaded);
            if (instance == null) return null;

            ApplyLayout(instance);
            Spin(instance);
            ctx.State.RegisterVfx(this, instance);
            return instance;
        }

        /// <summary>
        /// Quay đều quanh tâm, vô hạn. Cộng dồn lên góc nghiêng mà ApplyLayout vừa đặt chứ không
        /// ghi đè, để VFX chạy sau một lớp nghiêng vẫn giữ đúng độ nghiêng đó làm trục.
        /// </summary>
        private void Spin(RectTransform rect)
        {
            if (Mathf.Abs(spinDegreesPerSecond) <= MinSpinSpeed) return;

            float baseRotation = rect.localEulerAngles.z;
            float sweep = spinDegreesPerSecond > 0f ? 360f : -360f;
            float angle = 0f;

            DOTween.To(() => angle, value =>
                   {
                       angle = value;
                       rect.localRotation = Quaternion.Euler(0f, 0f, baseRotation + value);
                   }, sweep, 360f / Mathf.Abs(spinDegreesPerSecond))
                   .SetEase(Ease.Linear)
                   .SetLoops(-1, LoopType.Restart)
                   .SetTarget(rect)
                   .SetLink(rect.gameObject);
        }

        // Không đụng sizeDelta: particle bay theo local space của emitter, nới rect chỉ đổi
        // vùng mask chứ không làm hiệu ứng to ra.
        private void ApplyLayout(RectTransform rect)
        {
            if (!overrideLayout) return;
            rect.anchorMin = CenterAnchor;
            rect.anchorMax = CenterAnchor;
            rect.pivot = CenterAnchor;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }
}
