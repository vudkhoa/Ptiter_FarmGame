using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Preloading phase: load assets/splash/etc, sau đó LoadScene("Game").
    /// Start chạy sau RootLifetimeScope.Awake → container đã sẵn sàng.
    /// </summary>
    public sealed class PreloadingFlow : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private float _delaySeconds = 0.5f;

        private void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_delaySeconds));
            await SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Single);
        }
    }
}