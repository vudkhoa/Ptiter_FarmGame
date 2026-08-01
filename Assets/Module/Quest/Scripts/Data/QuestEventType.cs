namespace Core.Module.Quest
{
    public enum QuestEventType
    {
        // Kept at zero so existing StateReached quest assets deserialize safely.
        FarmStateReached = 0,
        FarmPlanted = 1,
        FarmCared = 2,
        FarmHarvestAction = 3,
        FarmHarvestItem = 4,
        FarmRipe = 5,
        FarmStageReached = 6,
        InventoryChanged = 100,
        CurrencyChanged = 101,
        FeatureUnlocked = 102
    }
}
