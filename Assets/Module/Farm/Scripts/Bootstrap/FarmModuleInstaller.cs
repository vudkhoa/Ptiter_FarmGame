using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Farm
{
    /// <summary>
    /// Khai báo mọi thứ module Farm cần để chạy.
    /// Farm chia 2 tầng: FarmService/Database là global (Root), các handler & view là per-scene (Game).
    /// </summary>
    public static class FarmModuleInstaller
    {
        private const string FarmDatabaseResourcePath = "FarmDatabase";

        /// <summary>Chỉ mở các kênh pub/sub (broker) do Farm sở hữu.</summary>
        public static IContainerBuilder RegisterFarmEvents(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<FarmSlotChangedPayload>(options);
            builder.RegisterMessageBroker<OpenFarmSelectorUIPayload>(options);
            return builder;
        }

        /// <summary>Broker + service global. Gọi ở ROOT scope.</summary>
        public static IContainerBuilder RegisterFarmModule(
            this IContainerBuilder builder,
            MessagePipeOptions options)
        {
            builder.RegisterFarmEvents(options);

            builder.RegisterInstance(LoadFarmDatabase());

            builder.Register<FarmService>(Lifetime.Singleton)
                   .AsImplementedInterfaces()
                   .AsSelf();

            return builder;
        }

        /// <summary>Component sống theo scene. Gọi ở GAME scope.</summary>
        public static IContainerBuilder RegisterFarmSceneComponents(this IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<FarmInputHandler>();
            builder.RegisterComponentInHierarchy<FarmVisualizer>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterComponentInHierarchy<FarmDebugLogger>();
            builder.RegisterComponentInHierarchy<FarmTestHelper>();
#endif

            return builder;
        }

        private static FarmDatabaseSO LoadFarmDatabase()
        {
            var database = Resources.Load<FarmDatabaseSO>(FarmDatabaseResourcePath);
            if (database == null)
            {
                Debug.LogWarning("[RootLifetimeScope] FarmDatabase SO not found in Resources. Creating a temporary runtime instance to prevent container crash. Make sure to place one at 'Assets/Resources/FarmDatabase.asset'.");
                database = ScriptableObject.CreateInstance<FarmDatabaseSO>();
            }
            return database;
        }
    }
}
