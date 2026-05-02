using UnityEngine;

namespace Core.Module.Input
{
    public interface IInputService
    {
        Vector2 PointerScreen { get; }
        bool IsPointerOverUI();
        bool IsButtonHeld(int button);
        bool IsKeyHeld(KeyCode key);
    }
}