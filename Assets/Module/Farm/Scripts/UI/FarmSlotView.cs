using System;
using Core.Module.Storage;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Farm
{
    [DisallowMultipleComponent]
    public sealed class FarmSlotView : MonoBehaviour
    {
        [Header("UI & Graphic References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private GameObject _feedBubble;      // Needs Food bubble
        [SerializeField] private GameObject _harvestBubble;   // Ready to Harvest bubble
        [SerializeField] private Transform _uiRoot;
        [SerializeField] private bool _useBillboard = true;
        [SerializeField, Min(0f)] private float _cropXSpacing = 0.4f;
        [SerializeField, Min(0f)] private float _cropZSpacing = 0.4f;
        [SerializeField] private int _cropSortingOrder = 1;
        [SerializeField] private int _animalSortingOrder = 1;

        [Header("Crop Motion")]
        [SerializeField] private FarmCropMotionSettings _motionSettings = new();

        [Header("Animal Motion")]
        [SerializeField] private FarmAnimalMotionSettings _animalMotionSettings = new();

        private SpriteRenderer[] _cropRenderers;
        private Quaternion[] _rendererRestRotations;
        private Vector3 _spriteBaseLocalPosition;
        private Vector3 _spriteBaseLocalScale;
        private Camera _camera;
        private Tween _motionTween;
        private Tween _idleTween;
        private bool _isCrop;

        private void Awake()
        {
            if (_spriteRenderer != null)
            {
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
                _spriteBaseLocalScale = _spriteRenderer.transform.localScale;
                _cropRenderers = new SpriteRenderer[4];
                _cropRenderers[0] = _spriteRenderer;
                _rendererRestRotations = new Quaternion[4];
                _rendererRestRotations[0] = _spriteRenderer.transform.localRotation;
            }

            _camera = Camera.main;
            if (_useBillboard)
            {
                if (_spriteRenderer != null)
                {
                    MatchCameraRotation(_spriteRenderer.transform);
                    _rendererRestRotations[0] = _spriteRenderer.transform.localRotation;
                }
                if (_uiRoot != null) MatchCameraRotation(_uiRoot);
            }
        }

        public void UpdateView(FarmSlotSaveData slot, FarmDatabaseSO database)
        {
            // 1. If slot data is null or completely empty (unplanted Soil / unoccupied Barn)
            if (slot == null || (slot.state == FarmSlotState.Empty && string.IsNullOrEmpty(slot.entityId)))
            {
                StopMotion();
                SetEntitySprite(null, false);
                if (_progressBar != null) _progressBar.gameObject.SetActive(false);
                if (_feedBubble != null) _feedBubble.SetActive(false);
                if (_harvestBubble != null) _harvestBubble.SetActive(false);
                return;
            }

            // Fetch ScriptableObject config to retrieve entity details dynamically
            var entity = database.GetEntityById(slot.entityId);
            if (entity == null) return;

            bool isAnimal = entity.entityType == FarmEntityType.Animal;
            _isCrop = !isAnimal;

            // 2. Resolve Slot States
            switch (slot.state)
            {
                case FarmSlotState.Empty:
                    // A purchased animal remains visible while it is hungry/waiting to be fed.
                    if (isAnimal && entity.growthSprites != null && entity.growthSprites.Length > 0)
                    {
                        int spriteIndex = slot.isAdult
                            ? Mathf.Max(0, entity.growthSprites.Length - 2)
                            : 0;

                        if (_spriteRenderer != null)
                            _spriteRenderer.sortingOrder = _animalSortingOrder;

                        SetEntitySprite(entity.growthSprites[spriteIndex], false);
                    }
                    else
                    {
                        SetEntitySprite(null, false);
                    }

                    if (_progressBar != null) _progressBar.gameObject.SetActive(false);
                    if (_harvestBubble != null) _harvestBubble.SetActive(false);

                    if (isAnimal && !slot.isFed)
                    {
                        if (_feedBubble != null) _feedBubble.SetActive(true);
                    }
                    else
                    {
                        if (_feedBubble != null) _feedBubble.SetActive(false);
                    }
                    break;

                case FarmSlotState.Growing:
                    if (_feedBubble != null) _feedBubble.SetActive(false);
                    if (_harvestBubble != null) _harvestBubble.SetActive(false);

                    Sprite[] growthSprites = entity.growthSprites;
                    float requiredTime = entity.processTime;
                    float stage2Threshold = entity.stage2Threshold;

                    float progress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;
                    progress = Mathf.Clamp01(progress);

                    // Apply Morphing Sprites (Stage 1 vs Stage 2)
                    if (growthSprites != null && growthSprites.Length > 0)
                    {
                        if (_spriteRenderer != null)
                        {
                            int spriteIndex;
                            if (isAnimal && slot.isAdult)
                            {
                                // Keep displaying the adult sprite for grown-up animals
                                spriteIndex = Mathf.Max(0, growthSprites.Length - 2);
                            }
                            else
                            {
                                spriteIndex = 0;
                                if (growthSprites.Length == 2)
                                {
                                    spriteIndex = 0;
                                }
                                else if (growthSprites.Length >= 3)
                                {
                                    spriteIndex = progress < stage2Threshold ? 0 : 1;
                                }
                            }

                            if (isAnimal) _spriteRenderer.sortingOrder = _animalSortingOrder;
                            SetEntitySprite(growthSprites[spriteIndex], !isAnimal);
                        }
                    }

                    // Update Progress Bar
                    if (_progressBar != null)
                    {
                        _progressBar.gameObject.SetActive(true);
                        _progressBar.value = progress;
                    }
                    break;

                case FarmSlotState.Ripe:
                    if (_feedBubble != null) _feedBubble.SetActive(false);
                    if (_progressBar != null) _progressBar.gameObject.SetActive(false);
                    if (_harvestBubble != null) _harvestBubble.SetActive(true);

                    // Get Ripe Sprite (Stage 3)
                    Sprite ripeSprite = null;
                    if (entity.growthSprites != null && entity.growthSprites.Length > 0)
                    {
                        ripeSprite = entity.growthSprites[entity.growthSprites.Length - 1];
                    }

                    if (_spriteRenderer != null && ripeSprite != null)
                    {
                        if (isAnimal) _spriteRenderer.sortingOrder = _animalSortingOrder;
                        SetEntitySprite(ripeSprite, !isAnimal);
                    }
                    break;
            }

            if (_isCrop) EnsureIdle();
            else EnsureAnimalIdle();
        }

        public void PlayAnimalEnter()
        {
            if (_isCrop || !CanAnimateAnimal()) return;

            StopMotion();
            ResetRendererTransforms();
            Transform target = _spriteRenderer.transform;
            Vector3 finalPosition = _spriteBaseLocalPosition;
            target.localScale = _spriteBaseLocalScale * _animalMotionSettings.EnterStartScale;
            target.localPosition = finalPosition + Vector3.down * _animalMotionSettings.EnterDrop;

            _motionTween = DOTween.Sequence()
                .Join(target.DOScale(_spriteBaseLocalScale, _animalMotionSettings.EnterDuration)
                    .SetEase(_animalMotionSettings.EnterEase))
                .Join(target.DOLocalMove(finalPosition, _animalMotionSettings.EnterDuration)
                    .SetEase(Ease.OutCubic))
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(FinishAnimalMotion);
        }

        public void PlayAnimalReaction()
        {
            PlayAnimalHop(
                _animalMotionSettings != null ? _animalMotionSettings.ReactionDuration : 0f,
                _animalMotionSettings != null ? _animalMotionSettings.ReactionHopHeight : 0f,
                _animalMotionSettings != null ? _animalMotionSettings.ReactionPopScale : 1f,
                null);
        }

        public void PlayAnimalCollect(Action onComplete)
        {
            PlayAnimalHop(
                _animalMotionSettings != null ? _animalMotionSettings.CollectDuration : 0f,
                _animalMotionSettings != null ? _animalMotionSettings.CollectHopHeight : 0f,
                _animalMotionSettings != null ? _animalMotionSettings.CollectPopScale : 1f,
                onComplete);
        }

        private void PlayAnimalHop(
            float duration,
            float hopHeight,
            float popScale,
            Action onComplete)
        {
            StopMotion();
            if (_isCrop || !CanAnimateAnimal() || duration <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            ResetRendererTransforms();
            Transform target = _spriteRenderer.transform;
            float halfDuration = duration * 0.5f;
            var hop = DOTween.Sequence()
                .Append(target.DOLocalMoveY(_spriteBaseLocalPosition.y + hopHeight, halfDuration)
                    .SetEase(_animalMotionSettings.ReactionEase))
                .Append(target.DOLocalMoveY(_spriteBaseLocalPosition.y, halfDuration)
                    .SetEase(Ease.InQuad));
            var pop = DOTween.Sequence()
                .Append(target.DOScale(_spriteBaseLocalScale * popScale, halfDuration)
                    .SetEase(Ease.OutQuad))
                .Append(target.DOScale(_spriteBaseLocalScale, halfDuration)
                    .SetEase(Ease.InQuad));

            _motionTween = DOTween.Sequence()
                .Join(hop)
                .Join(pop)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    FinishAnimalMotion();
                    onComplete?.Invoke();
                });
        }

        private void FinishAnimalMotion()
        {
            _motionTween = null;
            ResetRendererTransforms();
            EnsureAnimalIdle();
        }

        public void PlayPlant()
        {
            if (!_isCrop || !CanAnimate()) return;

            StopMotion();
            ResetRendererTransforms();
            var sequence = DOTween.Sequence();

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer == null || !renderer.gameObject.activeSelf) continue;

                Transform target = renderer.transform;
                Vector3 finalPosition = GetRestPosition(i);
                target.localScale = _spriteBaseLocalScale * _motionSettings.PlantStartScale;
                target.localPosition = finalPosition + Vector3.down * _motionSettings.PlantDrop;
                float delay = i * _motionSettings.PlantStagger;

                sequence.Insert(delay,
                    target.DOScale(_spriteBaseLocalScale, _motionSettings.PlantDuration)
                        .SetEase(_motionSettings.PlantEase));
                sequence.Insert(delay,
                    target.DOLocalMove(finalPosition, _motionSettings.PlantDuration)
                        .SetEase(Ease.OutCubic));
            }

            _motionTween = sequence
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    _motionTween = null;
                    EnsureIdle();
                });
        }

        public void PlayStageChange()
        {
            if (!_isCrop || !CanAnimate()) return;

            StopMotion();
            ResetRendererTransforms();
            var sequence = DOTween.Sequence();
            float halfDuration = _motionSettings.StageDuration * 0.5f;

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer == null || !renderer.gameObject.activeSelf) continue;

                Transform target = renderer.transform;
                var pulse = DOTween.Sequence()
                    .Append(target.DOScale(
                        _spriteBaseLocalScale * _motionSettings.StagePopScale,
                        halfDuration).SetEase(_motionSettings.StageEase))
                    .Append(target.DOScale(
                        _spriteBaseLocalScale,
                        halfDuration).SetEase(Ease.OutQuad));
                sequence.Insert(i * _motionSettings.StageStagger, pulse);
            }

            _motionTween = sequence
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    _motionTween = null;
                    EnsureIdle();
                });
        }

        public void PlayHarvest(Action onComplete)
        {
            StopMotion();
            if (!CanAnimate())
            {
                onComplete?.Invoke();
                return;
            }

            ResetRendererTransforms();
            var sequence = DOTween.Sequence();
            bool hasVisibleRenderer = false;

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer == null || !renderer.gameObject.activeSelf) continue;
                hasVisibleRenderer = true;

                Transform target = renderer.transform;
                float delay = i * _motionSettings.HarvestStagger;
                sequence.Insert(delay,
                    target.DOScale(Vector3.zero, _motionSettings.HarvestDuration)
                        .SetEase(_motionSettings.HarvestEase));
                sequence.Insert(delay,
                    target.DOLocalMoveY(
                        GetRestPosition(i).y + _motionSettings.HarvestLift,
                        _motionSettings.HarvestDuration)
                        .SetEase(Ease.OutQuad));
            }

            if (!hasVisibleRenderer)
            {
                onComplete?.Invoke();
                return;
            }

            _motionTween = sequence
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    _motionTween = null;
                    ResetRendererTransforms();
                    onComplete?.Invoke();
                });
        }

        private bool CanAnimate()
        {
            return _motionSettings != null && _motionSettings.Enabled &&
                   _spriteRenderer != null && _cropRenderers != null;
        }

        private bool CanAnimateAnimal()
        {
            return _animalMotionSettings != null && _animalMotionSettings.Enabled &&
                   _spriteRenderer != null && _spriteRenderer.gameObject.activeSelf;
        }

        private void EnsureAnimalIdle()
        {
            if (_isCrop || !CanAnimateAnimal() || _motionTween != null || _idleTween != null)
                return;

            ResetRendererTransforms();
            float phase = 0f;
            float cycleDuration = _animalMotionSettings.IdleCycleDuration;
            Transform target = _spriteRenderer.transform;

            _idleTween = DOTween
                .To(
                    () => phase,
                    value =>
                    {
                        phase = value;
                        float wave = 0.5f - 0.5f * Mathf.Cos(value);
                        float scale = Mathf.Lerp(1f, _animalMotionSettings.IdleScale, wave);
                        float bob = Mathf.Sin(value) * _animalMotionSettings.IdleBobHeight;
                        target.localScale = _spriteBaseLocalScale * scale;
                        target.localPosition = _spriteBaseLocalPosition + Vector3.up * bob;
                    },
                    Mathf.PI * 2f,
                    cycleDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void EnsureIdle()
        {
            if (!_isCrop || !CanAnimate() || _motionTween != null || _idleTween != null) return;

            ResetRendererTransforms();
            if (!HasVisibleRenderer()) return;

            float phase = 0f;
            float cycleDuration = _motionSettings.IdleHalfCycleDuration * 4f;
            float phasePerSecond = Mathf.PI * 2f / cycleDuration;
            _idleTween = DOTween
                .To(
                    () => phase,
                    value =>
                    {
                        phase = value;
                        ApplyIdlePose(value, phasePerSecond);
                    },
                    Mathf.PI * 2f,
                    cycleDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void ApplyIdlePose(float phase, float phasePerSecond)
        {
            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer == null || !renderer.gameObject.activeSelf) continue;

                float localPhase = phase - i * _motionSettings.IdleStagger * phasePerSecond;
                float breath = 0.5f - 0.5f * Mathf.Cos(localPhase);
                float scale = Mathf.Lerp(1f, _motionSettings.IdleScale, breath);
                float sway = Mathf.Sin(localPhase) * _motionSettings.IdleSwayAngle;

                Transform target = renderer.transform;
                target.localScale = _spriteBaseLocalScale * scale;
                target.localRotation = GetRestRotation(i) * Quaternion.Euler(0f, 0f, sway);
            }
        }

        private bool HasVisibleRenderer()
        {
            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer != null && renderer.gameObject.activeSelf) return true;
            }

            return false;
        }

        private void StopMotion()
        {
            _motionTween?.Kill();
            _motionTween = null;
            _idleTween?.Kill();
            _idleTween = null;
        }

        private void ResetRendererTransforms()
        {
            if (_cropRenderers == null) return;
            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                SpriteRenderer renderer = _cropRenderers[i];
                if (renderer == null) continue;
                renderer.transform.localScale = _spriteBaseLocalScale;
                renderer.transform.localPosition = GetRestPosition(i);
                renderer.transform.localRotation = GetRestRotation(i);
            }
        }

        private Quaternion GetRestRotation(int index)
        {
            if (_rendererRestRotations == null || index < 0 || index >= _rendererRestRotations.Length)
                return Quaternion.identity;
            return _rendererRestRotations[index];
        }

        private Vector3 GetRestPosition(int index)
        {
            if (!_isCrop) return _spriteBaseLocalPosition;

            float halfX = _cropXSpacing * 0.5f;
            float halfZ = _cropZSpacing * 0.5f;
            return index switch
            {
                0 => _spriteBaseLocalPosition + new Vector3(-halfX, 0f, -halfZ),
                1 => _spriteBaseLocalPosition + new Vector3( halfX, 0f, -halfZ),
                2 => _spriteBaseLocalPosition + new Vector3(-halfX, 0f,  halfZ),
                3 => _spriteBaseLocalPosition + new Vector3( halfX, 0f,  halfZ),
                _ => _spriteBaseLocalPosition
            };
        }

        private void SetEntitySprite(Sprite sprite, bool showCropCluster)
        {
            if (_spriteRenderer == null) return;

            if (_cropRenderers == null)
            {
                _cropRenderers = new SpriteRenderer[4];
                _cropRenderers[0] = _spriteRenderer;
                _rendererRestRotations = new Quaternion[4];
                _rendererRestRotations[0] = _spriteRenderer.transform.localRotation;
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
                _spriteBaseLocalScale = _spriteRenderer.transform.localScale;
            }

            if (showCropCluster) EnsureCropRenderers();

            int rendererCount = showCropCluster ? _cropRenderers.Length : 1;
            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                var renderer = _cropRenderers[i];
                if (renderer == null) continue;

                bool visible = i < rendererCount && sprite != null;
                renderer.sprite = visible ? sprite : null;
                renderer.gameObject.SetActive(visible);
            }
        }

        private void EnsureCropRenderers()
        {
            if (_cropRenderers == null)
            {
                _cropRenderers = new SpriteRenderer[4];
                _cropRenderers[0] = _spriteRenderer;
                _rendererRestRotations = new Quaternion[4];
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
                _spriteBaseLocalScale = _spriteRenderer.transform.localScale;
                _rendererRestRotations[0] = _spriteRenderer.transform.localRotation;
            }

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                bool wasCreated = false;
                if (_cropRenderers[i] == null)
                {
                    _cropRenderers[i] = Instantiate(_spriteRenderer, _spriteRenderer.transform.parent);
                    _cropRenderers[i].name = $"CropSprite_{i + 1}";
                    wasCreated = true;
                }

                _cropRenderers[i].transform.localPosition = GetRestPosition(i);
                if (wasCreated)
                {
                    if (_useBillboard) MatchCameraRotation(_cropRenderers[i].transform);
                    _rendererRestRotations[i] = _cropRenderers[i].transform.localRotation;
                }
                _cropRenderers[i].sortingOrder = _cropSortingOrder;
            }
        }

        private void MatchCameraRotation(Transform target)
        {
            if (_camera == null || target == null) return;
            target.rotation = _camera.transform.rotation;
        }

        private void OnDestroy()
        {
            StopMotion();
        }
    }
}
