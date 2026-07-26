namespace Core.Module.Farm
{
    // Holds the FarmDatabaseSO preloaded at boot; MapSceneBootstrap enqueues it into the game scope.
    public interface IFarmDatabaseProvider
    {
        FarmDatabaseSO Database { get; }
    }
}