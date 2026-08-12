using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Tutorial
{
    /// Full-screen dim that reports "not hit" inside one rect, so a tap there falls through to the
    /// map raycast below. The hole is punched in the RAYCAST only, never in the artwork.
    public sealed class TutorialDimMask : Image
    {
        private Rect _hole;
        private bool _hasHole;

        #region Public API
        /// <paramref name="canvasPoint"/> and <paramref name="size"/> are canvas-space, the units
        /// every other layout call in TutorialHandView already works in.
        public void SetHole(RectTransform canvasRect, Vector2 canvasPoint, Vector2 size)
        {
            if (canvasRect == null) return;

            Vector2 local = rectTransform.InverseTransformPoint(canvasRect.TransformPoint(canvasPoint));
            _hole = new Rect(local - size * 0.5f, size);
            _hasHole = true;
        }

        public void ClearHole() => _hasHole = false;

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.IsRaycastLocationValid(screenPoint, eventCamera)) return false;
            if (!_hasHole) return true;

            // Failing the conversion means the point is not on this rect at all, which the base
            // call already accepted - treat it as blocked rather than opening the whole screen.
            return !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       rectTransform, screenPoint, eventCamera, out Vector2 local)
                   || !_hole.Contains(local);
        }
        #endregion
    }
}
