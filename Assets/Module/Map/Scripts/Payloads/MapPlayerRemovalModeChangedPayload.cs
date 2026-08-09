using UnityEngine;

namespace Core.Module.Map
{
    public readonly struct MapPlayerRemovalModeChangedPayload
    {
        public readonly bool IsActive;

        public MapPlayerRemovalModeChangedPayload(bool isActive)
        {
            IsActive = isActive;
        }
    }

    public readonly struct MapPlayerRemoveOptionPayload
    {
        public readonly bool IsVisible;
        public readonly Vector2 ScreenPosition;
        public readonly Vector3 WorldPosition;

        public MapPlayerRemoveOptionPayload(
            bool isVisible,
            Vector2 screenPosition,
            Vector3 worldPosition)
        {
            IsVisible = isVisible;
            ScreenPosition = screenPosition;
            WorldPosition = worldPosition;
        }
    }
}
