using System;
using DG.Tweening;
using UnityEngine;

namespace Core.Module.Farm
{
    [Serializable]
    public sealed class FarmAnimalMotionSettings
    {
        [SerializeField] private bool _enabled = true;

        [Header("Enter")]
        [SerializeField, Min(0f)] private float _enterDuration = 0.32f;
        [SerializeField, Range(0f, 1f)] private float _enterStartScale = 0.35f;
        [SerializeField, Min(0f)] private float _enterDrop = 0.12f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;

        [Header("Idle")]
        [SerializeField, Min(0.1f)] private float _idleCycleDuration = 1.8f;
        [SerializeField, Min(0f)] private float _idleBobHeight = 0.025f;
        [SerializeField, Range(1f, 1.15f)] private float _idleScale = 1.025f;

        [Header("Feed / Ready")]
        [SerializeField, Min(0f)] private float _reactionDuration = 0.32f;
        [SerializeField, Min(0f)] private float _reactionHopHeight = 0.12f;
        [SerializeField, Min(1f)] private float _reactionPopScale = 1.12f;
        [SerializeField] private Ease _reactionEase = Ease.OutQuad;

        [Header("Collect Product")]
        [SerializeField, Min(0f)] private float _collectDuration = 0.38f;
        [SerializeField, Min(0f)] private float _collectHopHeight = 0.16f;
        [SerializeField, Min(1f)] private float _collectPopScale = 1.16f;

        public bool Enabled => _enabled;
        public float EnterDuration => _enterDuration;
        public float EnterStartScale => _enterStartScale;
        public float EnterDrop => _enterDrop;
        public Ease EnterEase => _enterEase;
        public float IdleCycleDuration => _idleCycleDuration;
        public float IdleBobHeight => _idleBobHeight;
        public float IdleScale => _idleScale;
        public float ReactionDuration => _reactionDuration;
        public float ReactionHopHeight => _reactionHopHeight;
        public float ReactionPopScale => _reactionPopScale;
        public Ease ReactionEase => _reactionEase;
        public float CollectDuration => _collectDuration;
        public float CollectHopHeight => _collectHopHeight;
        public float CollectPopScale => _collectPopScale;
    }
}
