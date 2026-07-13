namespace Core.Module.Storage
{
    public readonly struct InventoryChangedPayload
    {
        public readonly string ItemId;
        public readonly int NewAmount;
        public readonly int Delta;

        public InventoryChangedPayload(string itemId, int newAmount, int delta)
        {
            ItemId = itemId;
            NewAmount = newAmount;
            Delta = delta;
        }
    }
}
