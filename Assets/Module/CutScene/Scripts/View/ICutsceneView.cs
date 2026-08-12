using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    public interface ICutsceneView
    {
        CanvasGroup RootCanvasGroup { get; }

        /// <summary>Lấy 1 Image mới trong slot (từ pool hoặc Instantiate template).</summary>
        Image AcquireImage(CutsceneImageSlot slot);

        /// <summary>Trả Image về pool. Task chỉ được release Image do CHÍNH nó Acquire.</summary>
        void ReleaseImage(Image image);

        /// <summary>
        /// Spawn prefab VFX vào slot, dùng cho hiệu ứng mà Image tĩnh không làm được.
        /// Nằm cùng slot với Image nên thứ tự spawn quyết định lớp trên/dưới.
        /// </summary>
        RectTransform AcquireVfx(CutsceneImageSlot slot, GameObject prefab);

        /// <summary>Huỷ instance VFX. Task chỉ được release cái do CHÍNH nó Acquire.</summary>
        void ReleaseVfx(RectTransform instance);

        /// <summary>Trả tất cả Image đang active về pool trước khi cutscene mới bắt đầu.</summary>
        void ResetSlots();

        /// <summary>
        /// Button đã đăng ký sẵn trong prefab, tra theo id. Null nếu id không có.
        /// SO không tham chiếu thẳng Button được (runtime là clone), nên phải đi qua id.
        /// </summary>
        Button GetButton(string buttonId);
    }
}
