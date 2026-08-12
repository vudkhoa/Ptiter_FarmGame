using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// Canvases that must outrank a focused modal. The app promotes that modal to (highest root
    /// canvas + 10), which used to bury any step pointing at a widget INSIDE the modal.
    public static class TutorialOverlayCanvasRegistry
    {
        /// Gap kept between the raised modal and the lowest overlay canvas.
        private const int ModalPadding = 10;

        private static readonly List<Entry> Entries = new();

        private static int _offset;

        /// The offset rides on top of the authored order rather than replacing it, so the depth
        /// inside the container (dim below, foreground above) survives every raise.
        private readonly struct Entry
        {
            public readonly Canvas Canvas;
            public readonly int BaseOrder;

            public Entry(Canvas canvas, int baseOrder)
            {
                Canvas = canvas;
                BaseOrder = baseOrder;
            }
        }

        #region Public API
        // Registration
        public static void Register(Canvas canvas)
        {
            if (canvas == null) return;

            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Canvas == canvas) return;
            }

            // A newcomer has never been lifted by us, so whatever it carries now is its base.
            Entries.Add(new Entry(canvas, canvas.sortingOrder));
            Apply();
        }

        public static void Unregister(Canvas canvas)
        {
            if (canvas == null) return;

            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Canvas != canvas) continue;

                canvas.sortingOrder = Entries[i].BaseOrder;
                Entries.RemoveAt(i);
                return;
            }
        }

        /// True when this canvas is a tutorial overlay and must be left out of a max scan.
        public static bool IsOverlay(Canvas canvas)
        {
            if (canvas == null) return false;

            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Canvas == canvas) return true;
            }

            return false;
        }

        // Sorting
        /// Lifts every overlay so its lowest member clears <paramref name="sortingOrder"/>.
        public static void RaiseAbove(int sortingOrder)
        {
            int lowestBase = int.MaxValue;
            for (int i = 0; i < Entries.Count; i++)
            {
                lowestBase = Mathf.Min(lowestBase, Entries[i].BaseOrder);
            }

            if (lowestBase == int.MaxValue) return;

            SetOffset(Mathf.Max(0, sortingOrder + ModalPadding - lowestBase));
        }

        public static void ResetToBase() => SetOffset(0);
        #endregion

        #region Private Methods
        private static void SetOffset(int offset)
        {
            if (_offset == offset) return;

            _offset = offset;
            Apply();
        }

        private static void Apply()
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Canvas canvas = Entries[i].Canvas;
                if (canvas == null)
                {
                    // Destroyed with its scene without unregistering. Prune lazily.
                    Entries.RemoveAt(i);
                    continue;
                }

                int target = Entries[i].BaseOrder + _offset;
                if (canvas.sortingOrder != target) canvas.sortingOrder = target;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
            _offset = 0;
        }
        #endregion
    }
}
