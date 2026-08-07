using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Input
{
    /// <summary>
    /// Tracks full-screen modal windows that temporarily own all pointer input.
    /// Gameplay UI such as the farm selector deliberately does not register here.
    /// </summary>
    public static class GameplayInputBlockRegistry
    {
        private static readonly HashSet<object> Owners = new();

        public static bool IsBlocked => Owners.Count > 0;

        public static void Add(object owner)
        {
            if (owner != null)
                Owners.Add(owner);
        }

        public static void Remove(object owner)
        {
            if (owner != null)
                Owners.Remove(owner);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Owners.Clear();
        }
    }
}
