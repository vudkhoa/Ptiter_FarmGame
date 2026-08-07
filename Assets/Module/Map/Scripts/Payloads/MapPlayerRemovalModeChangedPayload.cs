namespace Core.Module.Map
{
    public readonly struct MapPlayerRemovalModeChangedPayload
    {
        public readonly bool IsActive;

        public MapPlayerRemovalModeChangedPayload(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
