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
                {
                    Debug.LogWarning("[GameLifetimeScope] RootLifetimeScope không tìm thấy. Hãy start từ scene Preloading.");
                }
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // TODO: register gameplay-only services với Lifetime.Scoped (EnemySpawner, WaveManager, ...).
        }
    }
}
