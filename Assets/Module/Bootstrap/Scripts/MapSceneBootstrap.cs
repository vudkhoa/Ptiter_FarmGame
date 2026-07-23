using System;
using Core.Module.Farm;
using Core.Module.Quest;
using Cysharp.Threading.Tasks;
using MyOwn.ServiceHarness;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace MyOwn.Bootstrap
{
    /// <summary>
    /// Gameplay scene entry point: load assets, build the scope, then open the scene gate.
    /// Requires GameLifetimeScope.autoRun to be OFF and _gameplayRoot to be INACTIVE in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSceneBootstrap : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private GameLifetimeScope _scope;
        [SerializeField] private AssetReference _farmDataRef;
        [SerializeField] private AssetReference _questCatalogRef; 

        [Header("Scene Gate")]
        [Tooltip("Root holding every object that needs injection. Keep it inactive; it is enabled once the container is built.")]
        [SerializeField] private GameObject _gameplayRoot;

        private void Start()
        {
            BuildScopeAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_farmDataRef != null && _farmDataRef.IsValid())
            {
                _farmDataRef.ReleaseAsset();
            }

            if (_questCatalogRef != null && _questCatalogRef.IsValid())
            {
                _questCatalogRef.ReleaseAsset();
            }
        }

        private async UniTaskVoid BuildScopeAsync()
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

            var farmDatabase = await LoadFarmDatabaseAsync();
            var questCatalogDatabase = await LoadQuestCatalogAsync();

            // Enqueue only applies to scopes built inside this using block.
            using (LifetimeScope.Enqueue(b =>
               {
                   b.RegisterInstance(farmDatabase);
                   b.RegisterInstance(questCatalogDatabase);
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

        #region Load Data
        private async UniTask<FarmDatabaseSO> LoadFarmDatabaseAsync()
        {
            FarmDatabaseSO database = null;

            if (_farmDataRef != null && _farmDataRef.RuntimeKeyIsValid())
            {
                try
                {
                    var handle = _farmDataRef.LoadAssetAsync<FarmDatabaseSO>();
                    await handle.ToUniTask();
                    database = handle.Result;

                }
                catch (Exception e)
                {
                    Debug.LogError($"[MapSceneBootstrap] Failed to load FarmDatabase: {e}");
                }
            }
            else
            {
                Debug.LogError("[MapSceneBootstrap] FarmDataRef is unassigned or its Addressables key is invalid.");
            }

            if (database == null)
            {
                // Still build with an empty database: losing Farm beats losing the whole scene.
                Debug.LogError("[MapSceneBootstrap] Running with an EMPTY FarmDatabase - nothing can be planted. Check FarmDataRef and the FarmData group.");
                database = ScriptableObject.CreateInstance<FarmDatabaseSO>();
            }

            return database;
        }

        private async UniTask<QuestCatalogSO> LoadQuestCatalogAsync()
        {
            QuestCatalogSO database = null;

            if (_questCatalogRef != null && _questCatalogRef.RuntimeKeyIsValid())
            {
                try
                {
                    var handle = _questCatalogRef.LoadAssetAsync<QuestCatalogSO>();
                    await handle.ToUniTask();
                    database = handle.Result;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MapSceneBootstrap] Failed to load QuestCatalog: {e}");
                }
            }
            else
            {
                Debug.LogError("[MapSceneBootstrap] QuestCatalogRef is unassigned or its Addressables key is invalid.");
            }

            if (database == null)
            {
                // Still build with an empty catalog: losing Quest beats losing the whole scene.
                Debug.LogError("[MapSceneBootstrap] Running with an EMPTY QuestCatalog - no quests will load. Check QuestCatalogRef and the Addressables group.");
                database = ScriptableObject.CreateInstance<QuestCatalogSO>();
            }

            return database;
        }
        #endregion
    }
}
