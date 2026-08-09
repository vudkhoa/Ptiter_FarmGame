using System;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Map
{
    [Serializable]
    public sealed class MapPlacementMotionSettings
    {
        [SerializeField] private bool _enabled = true;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float _placementDuration = 0.11f;
        [SerializeField, Range(0f, 1f)] private float _placementStartScale = 0.8f;
        [SerializeField, Min(0f)] private float _placementDrop = 0.035f;
        [SerializeField] private Ease _placementScaleEase = Ease.OutCubic;
        [SerializeField] private Ease _placementMoveEase = Ease.OutCubic;

        [Header("Removal")]
        [SerializeField, Min(0f)] private float _removalDuration = 0.22f;
        [SerializeField, Range(0f, 1f)] private float _removalAnticipationRatio = 0.2f;
        [SerializeField, Min(1f)] private float _removalOvershoot = 1.08f;
        [SerializeField, Min(0f)] private float _removalLift = 0.08f;
        [SerializeField] private Ease _removalAnticipationEase = Ease.OutQuad;
        [SerializeField] private Ease _removalShrinkEase = Ease.InCubic;

        public bool Enabled => _enabled;
        public float PlacementDuration => _placementDuration;
        public float PlacementStartScale => _placementStartScale;
        public float PlacementDrop => _placementDrop;
        public Ease PlacementScaleEase => _placementScaleEase;
        public Ease PlacementMoveEase => _placementMoveEase;
        public float RemovalDuration => _removalDuration;
        public float RemovalAnticipationRatio => _removalAnticipationRatio;
        public float RemovalOvershoot => _removalOvershoot;
        public float RemovalLift => _removalLift;
        public Ease RemovalAnticipationEase => _removalAnticipationEase;
        public Ease RemovalShrinkEase => _removalShrinkEase;
    }
}
