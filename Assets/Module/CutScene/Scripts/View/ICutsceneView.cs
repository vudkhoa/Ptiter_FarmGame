using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    public interface ICutsceneView
    {
        CanvasGroup RootCanvasGroup { get; }

        /// Lấy 1 Image mới trong slot (từ pool hoặc Instantiate template).
        Image AcquireImage(CutsceneImageSlot slot);

        /// Trả Image về pool. Task chỉ được release Image do CHÍNH nó Acquire.
        void ReleaseImage(Image image);

        /// Spawn prefab VFX vào slot. Nằm cùng slot với Image nên thứ tự spawn quyết định lớp
        /// trên/dưới.
        RectTransform AcquireVfx(CutsceneImageSlot slot, GameObject prefab);

        /// Huỷ instance VFX. Task chỉ được release cái do CHÍNH nó Acquire.
        void ReleaseVfx(RectTransform instance);

        /// Trả tất cả Image đang active về pool trước khi cutscene mới bắt đầu.
        void ResetSlots();

        /// Button đăng ký sẵn trong prefab, tra theo id. SO không tham chiếu thẳng Button được
        /// (runtime là clone), nên phải đi qua id.
        Button GetButton(string buttonId);
    }
}
