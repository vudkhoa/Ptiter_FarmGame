using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Preloading phase: await the boot sequence (Addressables init + preload), then open the game scene.
    /// Injected by RootLifetimeScope; Start runs after the container is built.
    /// </summary>
    public sealed class PreloadingFlow : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "MapScene";

        private ILoadingService _loading;

        [Inject]
        public void Construct(ILoadingService loading) => _loading = loading;

        private void Start() => RunAsync(this.GetCancellationTokenOnDestroy()).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            try
            {
                await _loading.RunBootSequenceAsync(ct);
                await SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Single)
                                  .ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Leaving the scene/app while loading - expected, not an error.
            }
            catch (Exception e)
            {
                Debug.LogError($"[PreloadingFlow] Boot sequence failed: {e}");
            }
        }
    }
}