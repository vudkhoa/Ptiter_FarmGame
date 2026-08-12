using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Maps a tutorial anchor id to something on screen. Static on purpose: anchors sit on
    /// prefabs instantiated at runtime (map objects, farm slots, UI spawned per window), which
    /// VContainer never injects. Mirrors GameplayInputBlockRegistry, including the domain-reload reset.
    /// </summary>
    public static class TutorialAnchorRegistry
    {
        private static readonly Dictionary<string, List<Transform>> Transforms = new();
        private static readonly Dictionary<string, WorldAnchor> WorldPoints = new();

        /// <summary>
        /// A pinned world position, optionally with the half-extent of the thing sitting there.
        /// The extent lets the view size a highlight from the real footprint instead of a guessed
        /// pixel size, so it keeps matching when the camera zooms.
        /// </summary>
        private readonly struct WorldAnchor
        {
            public readonly Vector3 Position;
            public readonly Vector3 HalfExtent;

            public WorldAnchor(Vector3 position, Vector3 halfExtent)
            {
                Position = position;
                HalfExtent = halfExtent;
            }
        }

        #region Transform anchors
        public static void Register(string anchorId, Transform target)
        {
            if (string.IsNullOrWhiteSpace(anchorId) || target == null) return;

            if (!Transforms.TryGetValue(anchorId, out List<Transform> list))
            {
                list = new List<Transform>();
                Transforms[anchorId] = list;
            }

            if (list.Contains(target)) return;
            list.Add(target);
        }

        public static void Unregister(string anchorId, Transform target)
        {
            if (string.IsNullOrWhiteSpace(anchorId) || target == null) return;
            if (!Transforms.TryGetValue(anchorId, out List<Transform> list)) return;

            if (!list.Remove(target)) return;
            if (list.Count == 0) Transforms.Remove(anchorId);
        }
        #endregion

        #region World point anchors
        /// <summary>
        /// Pin an anchor to a bare world position. Gameplay bridges use this for targets that
        /// have no dedicated component yet, such as the plot the player has just placed.
        /// </summary>
        public static void SetWorldPoint(string anchorId, Vector3 worldPosition)
            => SetWorldPoint(anchorId, worldPosition, Vector3.zero);

        /// <summary><paramref name="halfExtent"/> is half the world-space footprint; zero means unknown.</summary>
        public static void SetWorldPoint(string anchorId, Vector3 worldPosition, Vector3 halfExtent)
        {
            if (string.IsNullOrWhiteSpace(anchorId)) return;
            WorldPoints[anchorId] = new WorldAnchor(worldPosition, halfExtent);
        }

        /// <summary>Half the world footprint pinned with this anchor, or zero when none was given.</summary>
        public static Vector3 GetWorldHalfExtent(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId)) return Vector3.zero;
            return WorldPoints.TryGetValue(anchorId, out WorldAnchor anchor)
                ? anchor.HalfExtent
                : Vector3.zero;
        }

        public static void ClearWorldPoint(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId)) return;
            WorldPoints.Remove(anchorId);
        }
        #endregion

        /// <summary>
        /// Newest live registration wins: the last farm slot the player touched is the one
        /// the hand should point at. Returns false when nothing under this id is alive.
        /// </summary>
        public static bool TryResolve(string anchorId, out Transform target, out Vector3 worldPoint)
        {
            target = null;
            worldPoint = Vector3.zero;
            if (string.IsNullOrWhiteSpace(anchorId)) return false;

            if (Transforms.TryGetValue(anchorId, out List<Transform> list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Transform candidate = list[i];
                    if (candidate == null)
                    {
                        // Destroyed without unregistering (scene unload). Prune lazily.
                        list.RemoveAt(i);
                        continue;
                    }

                    if (!candidate.gameObject.activeInHierarchy) continue;

                    target = candidate;
                    worldPoint = candidate.position;
                    return true;
                }

                if (list.Count == 0) Transforms.Remove(anchorId);
            }

            if (WorldPoints.TryGetValue(anchorId, out WorldAnchor anchor))
            {
                worldPoint = anchor.Position;
                return true;
            }

            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Transforms.Clear();
            WorldPoints.Clear();
        }
    }
}
