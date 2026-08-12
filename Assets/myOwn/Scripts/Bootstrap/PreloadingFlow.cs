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
        private LoadingScreenView _screen;

        [Inject]
        public void Construct(ILoadingService loading, LoadingScreenView screen)
        {
            _loading = loading;
            _screen = screen;
        }

        private void Start() => RunAsync(this.GetCancellationTokenOnDestroy()).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            try
            {
                await _loading.RunBootSequenceAsync(ct);

                // A fast boot reports 100 within a couple of frames; without this the scene swaps
                // before the bar has travelled anywhere and the fill looks frozen at 0.
                if (_screen != null) await _screen.WaitUntilCaughtUpAsync(ct);

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