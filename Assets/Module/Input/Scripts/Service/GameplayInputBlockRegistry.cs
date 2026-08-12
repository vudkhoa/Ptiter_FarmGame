using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Input
{
    /// <summary>
    /// Tracks full-screen windows that temporarily own all pointer input.
    /// </summary>
    public static class GameplayInputBlockRegistry
    {
        private static readonly HashSet<object> Owners = new();
        private static object _lastOwner;

        public static bool IsBlocked => Owners.Count > 0;
        public static event Action<bool> BlockedStateChanged;

        /// <summary>
        /// The most recently opened modal component. The global Canvas blocker
        /// uses its parent as the popup layer, so it stays below modal content.
        /// </summary>
        public static Component CurrentOwner
        {
            get
            {
                if (_lastOwner is Component lastComponent &&
                    lastComponent != null)
                    return lastComponent;

                foreach (object owner in Owners)
                {
                    if (owner is not Component component || component == null)
                        continue;

                    _lastOwner = owner;
                    return component;
                }

                return null;
            }
        }

        public static void Add(object owner)
        {
            if (owner == null || !Owners.Add(owner)) return;

            _lastOwner = owner;
            BlockedStateChanged?.Invoke(true);
        }

        public static void Remove(object owner)
        {
            if (owner == null || !Owners.Remove(owner)) return;

            if (ReferenceEquals(_lastOwner, owner))
                _lastOwner = null;
            BlockedStateChanged?.Invoke(IsBlocked);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Owners.Clear();
            _lastOwner = null;
            BlockedStateChanged = null;
        }
    }
}
