using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Input
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Data/Input/Config")]
    public class InputConfigSO : ScriptableObject
    {
        [Space]
        [Header("Key Settings")]
        public List<KeyCode> WatchedKeys = new();

        [Space]
        [Header("Mouse Button Settings")]
        public float PointerMoveEpsilon = .5f;
    }
}