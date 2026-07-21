using System;
using Core.Module.Farm;
using Cysharp.Threading.Tasks;
using MyOwn.ServiceHarness;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.Bootstrap
{
    /// <summary>
    /// Điểm vào của scene gameplay: nạp asset bằng Addressables → build GameLifetimeScope → mở cổng scene.
    ///
    /// Hai điều kiện bắt buộc, sai một cái là scene chạy sai mà không báo:
    ///  1. GameLifetimeScope phải TẮT autoRun, nếu không nó tự build trước khi có database.
    ///  2. _gameplayRoot phải để INACTIVE sẵn trong scene, để Awake/Start của đám con chỉ chạy sau khi inject xong.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSceneBootstrap : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private GameLifetimeScope _scope;
        [SerializeField] private FarmDatabaseReference _farmDataRef;

        [Header("Scene Gate")]
        [Tooltip("Root chứa toàn bộ object cần inject. Để inactive trong scene; bật lên sau khi container build xong.")]
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
                Debug.LogError("[MapSceneBootstrap] Chưa gán GameLifetimeScope — scene sẽ không có container nào.");
                return;
            }

            // Root đang bật sẵn nghĩa là Awake/Start của đám con đã chạy trước khi container tồn tại:
            // field [Inject] còn null, và lỗi thường im lặng (`?.` nuốt mất) chứ không ném exception.
            if (_gameplayRoot != null && _gameplayRoot.activeSelf)
            {
                Debug.LogWarning($"[MapSceneBootstrap] '{_gameplayRoot.name}' đang bật sẵn trong scene. Tắt nó đi trong Inspector, nếu không mọi thứ inject đều chạy trước khi build.");
            }

            var database = await LoadFarmDatabaseAsync();

            // Enqueue chỉ áp cho scope build bên trong using; ra khỏi block là Pop khỏi stack tĩnh của VContainer.
            using (LifetimeScope.Enqueue(b => b.RegisterInstance(database)))
            {
                _scope.Build();
            }

            await UniTask.NextFrame();

            // Mở cổng: từ đây Awake/Start của đám con mới chạy, và chúng đã được inject lúc Build.
            if (_gameplayRoot != null)
            {
                _gameplayRoot.SetActive(true);
            }
            else
            {
                Debug.LogError("[MapSceneBootstrap] Chưa gán GameplayRoot — không mở được cổng scene, gameplay sẽ không chạy.");
            }
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
                    Debug.LogError($"[MapSceneBootstrap] Load FarmDatabase thất bại: {e}");
                }
            }
            else
            {
                Debug.LogError("[MapSceneBootstrap] FarmDataRef chưa gán hoặc key không hợp lệ.");
            }

            if (database == null)
            {
                // Vẫn build scope với database rỗng: mất Farm còn hơn mất cả scene (Map, Input, UI).
                Debug.LogError("[MapSceneBootstrap] Chạy với FarmDatabase RỖNG — không trồng được gì. Kiểm tra FarmDataRef và group FarmData.");
                database = ScriptableObject.CreateInstance<FarmDatabaseSO>();
            }

            return database;
        }
    }
}
