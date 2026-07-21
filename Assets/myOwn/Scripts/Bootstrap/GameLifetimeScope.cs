using Core.Module.Map;
using Core.Module.Farm;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Per-scene container. Auto-find RootLifetimeScope làm parent → inherit registrations.
    /// Pitfall: chạy thẳng scene Game → warning, Singletons từ Root không resolve được.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            if (parentReference.Object == null)
            {
                parentReference.Object = FindAnyObjectByType<RootLifetimeScope>();
                if (parentReference.Object == null)
                    Debug.LogWarning("[GameLifetimeScope] RootLifetimeScope không tìm thấy. Start từ Preloading.");
            }
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Component sống theo scene — mỗi module tự khai trong {Module}ModuleInstaller.cs.
            // Broker global đã đăng ký ở RootLifetimeScope, scope này kế thừa hết.
            // FarmDatabaseSO do MapSceneBootstrap Enqueue vào trước khi gọi Build().
            builder.RegisterMapSceneComponents()
                   .RegisterFarmGameplay();
        }
    }
}
