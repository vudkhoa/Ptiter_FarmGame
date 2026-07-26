namespace Core.Module.Quest
{
    // Holds the QuestCatalogSO preloaded at boot; MapSceneBootstrap enqueues it into the game scope.
    public interface IQuestCatalogProvider
    {
        QuestCatalogSO Catalog { get; }
    }
}