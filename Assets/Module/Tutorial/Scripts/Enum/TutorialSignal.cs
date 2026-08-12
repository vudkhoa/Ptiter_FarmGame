namespace Core.Module.Tutorial
{
    /// Gameplay moments a tutorial step can wait for. Explicit numbers: the values are serialized
    /// inside TutorialStepSO assets.
    public enum TutorialSignal
    {
        None = 0,

        /// The build menu (object picker) opened, so its rows can be pointed at.
        BuildMenuOpened = 5,

        /// Player tapped the build button and entered land placement mode.
        LandPlacementStarted = 10,

        /// Player confirmed a soil plot on the grid.
        LandPlaced = 11,

        /// The seed picker opened for an empty plot.
        SeedSelectorOpened = 20,

        /// A seed was committed into a plot.
        SeedPlanted = 21,

        /// A crop finished growing and can be harvested.
        CropRipe = 30,

        /// A crop was harvested.
        CropHarvested = 31,
    }
}
