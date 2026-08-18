namespace Core.Module.Tutorial
{
    /// Every anchor id the flows reference. Three assemblies register these, so a typo only ever
    /// surfaces as a hand that never appears.
    public static class TutorialAnchorIds
    {
        /// HUD button that opens the build menu. Lives on BuildingHudButton.prefab.
        public const string BuildButton = "tutorial.build_button";

        /// HUD button that opens the storage window.
        public const string InventoryButton = "tutorial.inventory_button";

        /// Root of the build menu, so a step can fade the whole panel out of the way.
        public const string ObjectsPanel = "tutorial.objects_panel";

        /// The soil row inside the build menu. Registered by the row that binds soil.
        public const string SoilButton = "tutorial.soil_button";

        /// First affordable seed inside the seed picker.
        public const string SeedItem = "tutorial.seed_item";

        /// An empty cell the player is actually allowed to build on.
        public const string FreePlot = "tutorial.free_plot";

        /// The plot the player has just placed (or the bare plot restored from a save).
        public const string NewPlot = "tutorial.new_plot";

        /// The crop that has just ripened.
        public const string RipeCrop = "tutorial.ripe_crop";
    }
}
