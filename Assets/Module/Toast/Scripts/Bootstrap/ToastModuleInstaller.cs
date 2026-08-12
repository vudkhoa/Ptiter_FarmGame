using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Module.Toast
{
    /// Service and view live at ROOT: a toast has to survive the map reload that raised it, and
    /// every layer of the game is allowed to raise one.
    public static class ToastModuleInstaller
    {
        /// Call at ROOT scope. A missing container costs the toasts, never the boot: the graph
        /// falls back to NullToastView so the rest of the game still resolves.
        public static IContainerBuilder RegisterToastModule(
            this IContainerBuilder builder, ToastUIContainer container)
        {
            if (container != null)
            {
                builder.RegisterInstance(container).AsImplementedInterfaces().AsSelf();
            }
            else
            {
                Debug.LogWarning(
                    "[ToastModuleInstaller] No ToastUIContainer on RootLifetimeScope - toasts are " +
                    "disabled. Run Tools/Toast/Rebuild Toast Content.");
                builder.Register<NullToastView>(Lifetime.Singleton).AsImplementedInterfaces();
            }

            // Entry point, not a lazy Singleton: nothing injects IToastService at boot, yet
            // ToastHub must be bound before the first screen opens.
            builder.RegisterEntryPoint<ToastService>().AsSelf();

            return builder;
        }
    }
}
