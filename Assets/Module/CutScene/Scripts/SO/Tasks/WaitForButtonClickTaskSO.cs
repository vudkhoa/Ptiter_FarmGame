using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// Đứng chờ tới khi người chơi bấm đúng button đã kéo vào CutsceneWindowController.
    /// Đặt task này ở step cuối = cutscene chỉ đóng khi người chơi bấm nút.
    /// </summary>
    [CreateAssetMenu(fileName = "WaitForButtonClickTask", menuName = "GDD/Cutscene/Task/Wait For Button Click")]
    public sealed class WaitForButtonClickTaskSO : CutsceneTaskSO
    {
        [Tooltip("Khớp đúng Id trong mảng Buttons của CutsceneWindowController.")]
        public string buttonId;

        public override async UniTask RunAsync(CutsceneContext ctx, CancellationToken ct)
        {
            Button button = ctx.View?.GetButton(buttonId);
            if (button == null)
            {
                Debug.LogError(
                    $"[WaitForButtonClickTask] '{name}' không tìm thấy button id '{buttonId}' - " +
                    "kiểm tra mảng Buttons trong CutsceneWindowController.", this);
                return;
            }

            button.gameObject.SetActive(true);
            try
            {
                await button.OnClickAsync(ct);
            }
            finally
            {
                // Bị skip hoặc huỷ giữa chừng vẫn phải ẩn nút, nếu không lần chiếu sau nó hiện sẵn.
                if (button != null) button.gameObject.SetActive(false);
            }
        }
    }
}
