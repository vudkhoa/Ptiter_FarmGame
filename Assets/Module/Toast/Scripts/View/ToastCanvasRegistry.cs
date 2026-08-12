using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Toast
{
    /// A toast sits at a fixed high sorting order and never chases anything, so it only needs to
    /// be excluded from GlobalModalInputBlocker's "highest canvas" scan.
    public static class ToastCanvasRegistry
    {
        /// Above the UIManager canvas (0), the HUD (100-110) and the tutorial overlay (199-201),
        /// with room left for a modal raise on top of any of them.
        public const int ToastSortingOrder = 30000;

        private static readonly List<Canvas> Canvases = new();

        #region Public API
        public static void Register(Canvas canvas)
        {
            if (canvas == null || Canvases.Contains(canvas)) return;

            Canvases.Add(canvas);
        }

        public static void Unregister(Canvas canvas)
        {
            if (canvas == null) return;

            Canvases.Remove(canvas);
        }

        /// True when this canvas belongs to the toast layer and must be left out of a max scan.
        public static bool IsOverlay(Canvas canvas)
        {
            if (canvas == null) return false;

            for (int i = 0; i < Canvases.Count; i++)
            {
                if (Canvases[i] == canvas) return true;
            }

            return false;
        }
        #endregion

        #region Private Methods
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Canvases.Clear();
        #endregion
    }
}
