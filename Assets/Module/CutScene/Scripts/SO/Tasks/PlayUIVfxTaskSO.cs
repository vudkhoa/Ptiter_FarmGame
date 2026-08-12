using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Module.Cutscene
{
    /// Spawn prefab VFX vào slot ảnh cho thứ Image tĩnh không diễn được. Lớp trên/dưới theo đúng
    /// luật của ShowImageTaskSO - ai spawn trước nằm dưới.
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

        [Tooltip("Góc nghiêng quanh trục Z, đơn vị độ.")]
        public float rotationZ;

        [Tooltip("0 = nhả quyền điều khiển ngay. Khác 0 = giữ step lại chừng đó giây.")]
        [Min(0f)] public float holdSeconds;

        [Header("Xoay vòng quanh tâm")]
        [Tooltip("Độ/giây quanh trục Z, quay tới lúc cutscene đóng. Không punch scale được ở đây: " +
                 "UIParticle giữ quyền ghi localScale và ghi đè mọi tween scale từ ngoài.")]
        public float spinDegreesPerSecond;

        #region Public API
        public override async UniTask PrepareAsync(CutsceneContext ctx, CancellationToken ct)
        {
            if (prefab == null || !prefab.RuntimeKeyIsValid()) return;

            try
            {
                GameObject loaded = await ctx.Loader.LoadTrackerAsync(prefab);
                ctx.State.SetPreparedAsset(this, loaded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayUIVfxTaskSO] Load prefab thất bại ({name}): {e.Message}");
            }
        }

        public override UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            RectTransform instance = Spawn(ctx);
            if (instance == null || holdSeconds <= 0f) return UniTask.CompletedTask;

            return UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: ct);
        }

        /// Cố tình để trống. Complete chỉ chạy khi cutscene bị skip/huỷ, mà ngay sau đó là
        /// Hide window + ReleaseAll - spawn quầng sáng ra để huỷ liền là làm việc thừa.
        public override void Complete(CutsceneContext ctx) { }

        public override void Release(CutsceneContext ctx)
        {
            RectTransform instance = ctx.State.GetVfxByTask(this);
            if (instance == null) return;

            // Giết vòng xoay trước khi View Destroy: SetLink dọn được, nhưng nó chờ tới frame sau
            // mới thấy GameObject chết, còn đây thì tween buông rect ngay tại chỗ.
            DOTween.Kill(instance);
            ctx.View?.ReleaseVfx(instance);
        }
        #endregion

        #region Private Methods
        private RectTransform Spawn(CutsceneContext ctx)
        {
            GameObject loaded = ctx.State.GetPreparedAsset<GameObject>(this);
            if (loaded == null) return null;

            RectTransform instance = ctx.View?.AcquireVfx(slot, loaded);
            if (instance == null) return null;

            ApplyLayout(instance);
            Spin(instance);
            ctx.State.RegisterVfx(this, instance);
            return instance;
        }

        /// Quay đều quanh tâm, vô hạn. Cộng dồn lên góc nghiêng mà ApplyLayout vừa đặt chứ không
        /// ghi đè, để VFX chạy sau một lớp nghiêng vẫn giữ đúng độ nghiêng đó làm trục.
        private void Spin(RectTransform rect)
        {
            if (Mathf.Abs(spinDegreesPerSecond) <= MinSpinSpeed) return;

            float baseRotation = rect.localEulerAngles.z;
            float sweep = spinDegreesPerSecond > 0f ? 360f : -360f;
            float duration = 360f / Mathf.Abs(spinDegreesPerSecond);

            rect.DOLocalRotate(new Vector3(0f, 0f, baseRotation + sweep), duration, RotateMode.FastBeyond360)
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
        #endregion
    }
}
