using Core.Module.Map;
using Core.Module.Farm;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Core.Module.Quest.View;
#endif
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
            // Map module — scoped per-scene
            builder.RegisterComponentInHierarchy<MapService>()
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.RegisterComponentInHierarchy<MapPointerBridge>();
            builder.RegisterComponentInHierarchy<MapPreviewView>();

            // Farm module interactions — scoped per-scene
            builder.RegisterComponentInHierarchy<FarmInputHandler>();
            builder.RegisterComponentInHierarchy<FarmVisualizer>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterComponentInHierarchy<FarmDebugLogger>();
            builder.RegisterComponentInHierarchy<FarmTestHelper>();
            builder.RegisterComponentInHierarchy<QuestTestPanelView>();
#endif
        }
    }
}
