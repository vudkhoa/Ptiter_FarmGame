using UnityEngine;

namespace Core.Module.Input
{
    public readonly struct KeyDownPayload
    {
        public readonly KeyCode Key;

        public KeyDownPayload(KeyCode key)
        {
            Key = key;
        }
    }
}