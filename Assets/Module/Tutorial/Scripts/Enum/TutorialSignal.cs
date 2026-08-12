namespace Core.Module.Tutorial
{
    /// <summary>
    /// Gameplay moments a tutorial step can wait for. TutorialGameplaySignalBridge is the
    /// only translator from module payloads to these values, so steps never reference Farm/Map types.
    /// Explicit numbers: the values are serialized inside TutorialStepSO assets.
    /// </summary>
    public enum TutorialSignal
    {
        None = 0,

        /// <summary>Player tapped the build button and entered land placement mode.</summary>
        LandPlacementStarted = 10,

        /// <summary>Player confirmed a soil plot on the grid.</summary>
        LandPlaced = 11,

        /// <summary>The seed picker opened for an empty plot.</summary>
        SeedSelectorOpened = 20,

        /// <summary>A seed was committed into a plot.</summary>
        SeedPlanted = 21,

        /// <summary>A crop finished growing and can be harvested.</summary>
        CropRipe = 30,

        /// <summary>A crop was harvested.</summary>
        CropHarvested = 31,
    }
}
