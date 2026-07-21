using System.Collections.Generic;

namespace Core.Module.Farm
{
    /// <summary>
    /// Save data source for FarmService, implemented by the app layer.
    /// Keeps Farm unaware of PlayerDataHolder: the dependency only points MyOwn to Farm.
    /// </summary>
    public interface IFarmSaveSource
    {
        /// <summary>The live list from save data - FarmService keeps this reference so edits write straight back.</summary>
        List<FarmSlotSaveData> FarmSlots { get; }

        long LastSaveUtcTicks { get; }
    }
}
