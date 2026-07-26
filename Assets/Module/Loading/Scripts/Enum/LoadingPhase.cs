namespace Core.Module.Loading
{
    // Define Flow Loading
    public enum LoadingPhase
    {
        Initialize = 0,      // Addressables.InitializeAsync
        CheckCatalog = 1,    // CheckForCatalogUpdates / UpdateCatalogs
        Download = 2,        // GetDownloadSize + DownloadDependencies
        PreloadContent = 3,  // load FarmDatabase + furniture vào catalog
        Complete = 4
    }
}