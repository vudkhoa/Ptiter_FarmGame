using System.Collections.Generic;

namespace Core.Module.Farm
{
    /// <summary>
    /// Nguồn save cho FarmService, do tầng app cài đặt.
    /// Tách interface để Farm không phải biết tới PlayerDataHolder — chiều phụ thuộc là MyOwn → Farm.
    /// </summary>
    public interface IFarmSaveSource
    {
        /// <summary>List gốc trong save data. FarmService giữ nguyên tham chiếu này để thay đổi ghi ngược về PlayerData.</summary>
        List<FarmSlotSaveData> FarmSlots { get; }

        long LastSaveUtcTicks { get; }
    }
}
