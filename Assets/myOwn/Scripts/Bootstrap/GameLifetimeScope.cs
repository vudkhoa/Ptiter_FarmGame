using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// Per-scene container cho gameplay (scene "Game"). Parent = RootLifetimeScope (auto-find).
    /// Đặt component này trên GameObject "[Game Bootstrap]" trong scene Game.
    /// </summary>
    /// <remarks>
    /// Auto-find parent:
    /// - Awake() set parentReference.Object = FindAnyObjectByType<RootLifetimeScope>() TRƯỚC base.Awake().
    /// - Nhờ vậy Inspector field "Parent" để trống vẫn hoạt động khi vào từ Preloading.
    /// - Pitfall: vào thẳng Game scene → log warning, container vẫn build nhưng KHÔNG resolve được PlayerDataHolder/ClockService (Singleton từ Root).
    /// Configure: hiện để trống — thêm gameplay services Lifetime.Scoped sau (EnemySpawner, WaveManager, ...).
    /// </remarks>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            if (parentReference.Object == null)
            {
                parentReference.Object = FindAnyObjectByType<RootLifetimeScope>();
                if (parentReference.Object == null)
                {
                    Debug.LogWarning(
                        "[GameLifetimeScope] RootLifetimeScope không tìm thấy. " +
                        "Bạn đang chạy thẳng scene Game? Hãy start từ scene Preloading. " +
                        "Singletons từ Root (PlayerDataHolder, ClockService) sẽ không resolve được."
                    );
                }
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // TODO: register gameplay-only services với Lifetime.Scoped tại đây.
            // Ví dụ:
            //   builder.Register<EnemySpawner>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }
    }
}
