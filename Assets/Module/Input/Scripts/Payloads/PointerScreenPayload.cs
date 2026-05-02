namespace Core.Module.Input
{
    public readonly struct PointerScreenPayload
    {
        public readonly UnityEngine.Vector2 ScreenPosition;

        public PointerScreenPayload(UnityEngine.Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
        }
    }
}