using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Đại diện cho preloading phase: load assets, asset bundle, splash, etc.
    /// Sau khi xong → load Game scene.
    /// </summary>
    /// <remarks>
    /// Đặt component này trên GameObject bất kỳ trong scene Preloading.
    /// Chạy SAU RootLifetimeScope đã build container (vì Start chạy sau Awake).
    /// </remarks>
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
