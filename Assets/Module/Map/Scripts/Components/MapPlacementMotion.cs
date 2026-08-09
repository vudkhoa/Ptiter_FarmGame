using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Module.Map
{
    /// <summary>
    /// Stateless DOTween motion shared by every placed map object. Keeping this as
    /// a utility avoids runtime component creation and component lookups.
    /// </summary>
    public static class MapPlacementMotion
    {
        // Map interaction runs on Unity's main thread, so these buffers can be
        // reused safely and avoid allocating arrays for every removal.
        private static readonly List<Collider> Colliders3D = new();
        private static readonly List<Collider2D> Colliders2D = new();

        public static void PlayPlacement(
            GameObject gameObject,
            MapPlacementMotionSettings settings)
        {
            if (gameObject == null || settings == null || !settings.Enabled) return;

            Transform target = gameObject.transform;
            DOTween.Kill(target);
            Vector3 finalScale = target.localScale;
            Vector3 finalPosition = target.localPosition;
            target.localScale = finalScale * settings.PlacementStartScale;
            target.localPosition = finalPosition + Vector3.down * settings.PlacementDrop;

            DOTween.Sequence()
                .Join(target.DOScale(finalScale, settings.PlacementDuration)
                    .SetEase(settings.PlacementScaleEase))
                .Join(target.DOLocalMove(finalPosition, settings.PlacementDuration)
                    .SetEase(settings.PlacementMoveEase))
                .SetUpdate(true)
                .SetTarget(target);
        }

        public static void PlayRemoval(
            GameObject gameObject,
            MapPlacementMotionSettings settings)
        {
            if (gameObject == null) return;

            Transform target = gameObject.transform;
            DOTween.Kill(target);
            DisableInteraction(gameObject);

            if (settings == null || !settings.Enabled || settings.RemovalDuration <= 0f)
            {
                Object.Destroy(gameObject);
                return;
            }

            Vector3 startScale = target.localScale;
            Vector3 startPosition = target.localPosition;
            float anticipationDuration = settings.RemovalDuration * settings.RemovalAnticipationRatio;
            float shrinkDuration = settings.RemovalDuration - anticipationDuration;

            DOTween.Sequence()
                .Append(target.DOScale(startScale * settings.RemovalOvershoot, anticipationDuration)
                    .SetEase(settings.RemovalAnticipationEase))
                .Append(target.DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(settings.RemovalShrinkEase))
                .Join(target.DOLocalMoveY(startPosition.y + settings.RemovalLift, shrinkDuration)
                    .SetEase(settings.RemovalShrinkEase))
                .SetUpdate(true)
                .SetTarget(target)
                .OnComplete(() => Object.Destroy(gameObject));
        }

        public static void Stop(GameObject gameObject)
        {
            if (gameObject != null) DOTween.Kill(gameObject.transform);
        }

        private static void DisableInteraction(GameObject gameObject)
        {
            Colliders3D.Clear();
            gameObject.GetComponentsInChildren(true, Colliders3D);
            for (int i = 0; i < Colliders3D.Count; i++) Colliders3D[i].enabled = false;

            Colliders2D.Clear();
            gameObject.GetComponentsInChildren(true, Colliders2D);
            for (int i = 0; i < Colliders2D.Count; i++) Colliders2D[i].enabled = false;
        }
    }
}
