QuestCatalog.asset lives here because RootLifetimeScope loads it at runtime with
Resources.Load<QuestCatalogSO>("QuestCatalog"). Keep only the runtime lookup entry
point here; individual QuestDefinitionSO assets belong in ../Configs.
