#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Core.Module.Farm
{
    /// <summary>
    /// Component hỗ trợ kiểm thử PlayMode: In log màu ra Console khi nhận được sự kiện gieo trồng/thu hoạch/mở UI.
    /// Kéo vào chung GameObject với FarmInputHandler.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmDebugLogger : MonoBehaviour
    {
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(
            ISubscriber<OpenFarmSelectorUIPayload> openUiSub,
            ISubscriber<FarmSlotChangedPayload> slotChangedSub)
        {
            var bag = DisposableBag.CreateBuilder();

            // 1. Lắng nghe yêu cầu mở bảng chọn hạt
            openUiSub.Subscribe(payload =>
            {
                string typeName = payload.IsAnimal ? "Chuồng nuôi (Barn)" : "Ô đất trồng (Soil)";
                Debug.Log($"<color=cyan><b>[DEBUG FARM] Chạm ô trống thành công!</b></color>\n" +
                          $"Yêu cầu mở bảng UI tại ô Grid: {payload.Cell} | Loại: {typeName}");
            }).AddTo(bag);

            // 2. Lắng nghe trạng thái sinh trưởng của ô ruộng thay đổi
            slotChangedSub.Subscribe(payload =>
            {
                var slot = payload.Slot;
                Debug.Log($"<color=yellow><b>[DEBUG FARM] Trạng thái ô đất thay đổi!</b></color>\n" +
                          $"Tọa độ: ({slot.cellX}, {slot.cellY}, {slot.cellZ}) | Thực thể: {slot.entityId} | Trạng thái: {slot.state} | Tiến độ: {slot.growthTimeSec}s | Đã ăn: {slot.isFed}");
            }).AddTo(bag);

            _subscriptions = bag.Build();
        }

        private void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
    }
}
#endif
