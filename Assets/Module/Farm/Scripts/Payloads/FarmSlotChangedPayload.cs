namespace Core.Module.Farm
{
    public readonly struct FarmSlotChangedPayload
    {
        public readonly FarmSlotSaveData Slot;

        public FarmSlotChangedPayload(FarmSlotSaveData slot)
        {
            Slot = slot;
        }
    }
}
