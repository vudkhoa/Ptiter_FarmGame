using System;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Farm
{
    [Serializable]
    public sealed class FarmCropMotionSettings
    {
        [SerializeField] private bool _enabled = true;

        [Header("Plant")]
        [SerializeField, Min(0f)] private float _plantDuration = 0.28f;
        [SerializeField, Min(0f)] private float _plantDrop = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _plantStartScale = 0.05f;
        [SerializeField, Min(0f)] private float _plantStagger = 0.035f;
        [SerializeField] private Ease _plantEase = Ease.OutBack;

        [Header("Idle")]
        [SerializeField, Min(0.1f)] private float _idleHalfCycleDuration = 1.2f;
        [SerializeField, Range(1f, 1.2f)] private float _idleScale = 1.025f;
        [SerializeField, Range(0f, 10f)] private float _idleSwayAngle = 2.5f;
        [SerializeField, Min(0f)] private float _idleStagger = 0.08f;

        [Header("Ripe Bounce")]
        [SerializeField, Min(0.5f)] private float _ripeCycleDuration = 1.8f;
        [SerializeField, Range(0f, 15f)] private float _ripeSwayAngle = 7f;
        [SerializeField, Min(0.05f)] private float _ripeStretchDuration = 0.25f;
        [SerializeField, Range(0f, 0.6f)] private float _ripeStretchAmount = 0.35f;

        [Header("Stage Change")]
        [SerializeField, Min(0f)] private float _stageDuration = 0.24f;
        [SerializeField, Min(1f)] private float _stagePopScale = 1.2f;
        [SerializeField, Min(0f)] private float _stageStagger = 0.025f;
        [SerializeField] private Ease _stageEase = Ease.OutBack;

        [Header("Harvest")]
        [SerializeField, Min(0f)] private float _harvestDuration = 0.3f;
        [SerializeField, Min(0f)] private float _harvestLift = 0.2f;
        [SerializeField, Min(0f)] private float _harvestStagger = 0.03f;
        [SerializeField] private Ease _harvestEase = Ease.InBack;

        public bool Enabled => _enabled;
        public float PlantDuration => _plantDuration;
        public float PlantDrop => _plantDrop;
        public float PlantStartScale => _plantStartScale;
        public float PlantStagger => _plantStagger;
        public Ease PlantEase => _plantEase;
        public float IdleHalfCycleDuration => _idleHalfCycleDuration;
        public float IdleScale => _idleScale;
        public float IdleSwayAngle => _idleSwayAngle;
        public float IdleStagger => _idleStagger;
        public float RipeCycleDuration => _ripeCycleDuration;
        public float RipeSwayAngle => _ripeSwayAngle;
        public float RipeStretchDuration => _ripeStretchDuration;
        public float RipeStretchAmount => _ripeStretchAmount;
        public float StageDuration => _stageDuration;
        public float StagePopScale => _stagePopScale;
        public float StageStagger => _stageStagger;
        public Ease StageEase => _stageEase;
        public float HarvestDuration => _harvestDuration;
        public float HarvestLift => _harvestLift;
        public float HarvestStagger => _harvestStagger;
        public Ease HarvestEase => _harvestEase;
    }
}
