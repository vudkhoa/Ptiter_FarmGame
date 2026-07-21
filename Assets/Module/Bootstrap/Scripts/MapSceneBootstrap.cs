using System;
using Core.Module.Farm;
using Cysharp.Threading.Tasks;
using MyOwn.ServiceHarness;
using UnityEngine;
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
        [SerializeField] private FarmDatabaseReference _farmDataRef;

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

            var database = await LoadFarmDatabaseAsync();

            // Enqueue only applies to scopes built inside this using block.
            using (LifetimeScope.Enqueue(b => b.RegisterInstance(database)))
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

        private async UniTask<FarmDatabaseSO> LoadFarmDatabaseAsync()
        {
            FarmDatabaseSO database = null;

            if (_farmDataRef != null && _farmDataRef.RuntimeKeyIsValid())
            {
                try
                {
                    database = await _farmDataRef.LoadAssetAsync();
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
    }
}
