using Core.Module.Farm;
using Core.Module.Quest;
using MyOwn.ServiceHarness;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.Bootstrap
{
    /// <summary>
    /// Gameplay scene entry point: pull the boot-preloaded data from the root container,
    /// build the game scope, then open the scene gate.
    /// Requires GameLifetimeScope.autoRun OFF and _gameplayRoot INACTIVE in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSceneBootstrap : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private GameLifetimeScope _scope;

        [Header("Scene Gate")]
        [Tooltip("Root holding every object that needs injection. Keep it inactive; enabled once the container is built.")]
        [SerializeField] private GameObject _gameplayRoot;

        private void Start() => BuildScope();

        private void BuildScope()
        {
            if (_scope == null)
            {
                Debug.LogError("[MapSceneBootstrap] Scope field is empty - this scene will have no container at all.");
                return;
            }

            // An already-active root means its children ran Awake/Start before injection, which fails silently.
            if (_gameplayRoot != null && _gameplayRoot.activeSelf)
            {
                Debug.LogWarning($"[MapSceneBootstrap] '{_gameplayRoot.name}' is active in the scene - turn it off, otherwise injected objects start before the container exists.");
            }

            // This object runs before its own scope is built, so it isn't auto-injected.
            // Pull the boot-preloaded data straight from the root (global) container.
            var root = FindAnyObjectByType<RootLifetimeScope>();
            if (root == null)
            {
                Debug.LogError("[MapSceneBootstrap] No RootLifetimeScope found - start the game from the Preloading scene.");
                return;
            }

            var farmDatabase = root.Container.Resolve<IFarmDatabaseProvider>().Database;
            var questCatalog = root.Container.Resolve<IQuestCatalogProvider>().Catalog;

            // Fail-fast: don't enter gameplay with missing data (avoids overwriting a valid save with empty state).
            if (farmDatabase == null)
            {
                Debug.LogError("[MapSceneBootstrap] FarmDatabase was not preloaded - blocking gameplay.");
                return;
            }
            if (questCatalog == null)
            {
                Debug.LogError("[MapSceneBootstrap] QuestCatalog was not preloaded - blocking gameplay.");
                return;
            }

            // Enqueue only applies to scopes built inside this using block.
            using (LifetimeScope.Enqueue(b =>
            {
                b.RegisterInstance(farmDatabase);
                b.RegisterInstance(questCatalog);
            }))
            {
                _scope.Build();
            }

            if (_gameplayRoot == null)
            {
                Debug.LogError("[MapSceneBootstrap] GameplayRoot field is empty - the scene gate never opens and gameplay stays dead.");
                return;
            }

            // Opening the gate: children now run Awake/Start with their dependencies already injected.
            _gameplayRoot.SetActive(true);
        }
    }
}