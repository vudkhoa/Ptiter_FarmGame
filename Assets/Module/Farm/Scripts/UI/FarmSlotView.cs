using Core.Module.Storage;
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
        [SerializeField] private bool _useBillboard = true;
        [SerializeField, Min(0f)] private float _cropHorizontalSpacing = 0.4f;
        [SerializeField, Min(0f)] private float _cropVerticalSpacing = 0.22f;
        [SerializeField] private int _cropSortingOrder = 1;

        private SpriteRenderer[] _cropRenderers;
        private Vector3 _spriteBaseLocalPosition;
        private Camera _camera;

        private void Awake()
        {
            if (_spriteRenderer != null)
            {
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
                _cropRenderers = new SpriteRenderer[4];
                _cropRenderers[0] = _spriteRenderer;
            }

            _camera = Camera.main;
            if (_useBillboard) FaceCamera();
        }

        public void UpdateView(FarmSlotSaveData slot, FarmDatabaseSO database)
        {
            // 1. If slot data is null or completely empty (unplanted Soil / unoccupied Barn)
            if (slot == null || (slot.state == FarmSlotState.Empty && string.IsNullOrEmpty(slot.entityId)))
            {
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

            // 2. Resolve Slot States
            switch (slot.state)
            {
                case FarmSlotState.Empty:
                    // If it is an adult animal, keep displaying the adult sprite instead of null
                    if (isAnimal && slot.isAdult)
                    {
                        if (entity.growthSprites != null && entity.growthSprites.Length > 0)
                        {
                            int lastIdx = entity.growthSprites.Length - 1;
                            SetEntitySprite(entity.growthSprites[lastIdx], false);
                        }
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
                                spriteIndex = growthSprites.Length - 1;
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
                        SetEntitySprite(ripeSprite, !isAnimal);
                    }
                    break;
            }
        }

        private void SetEntitySprite(Sprite sprite, bool showCropCluster)
        {
            if (_spriteRenderer == null) return;

            if (_cropRenderers == null)
            {
                _cropRenderers = new SpriteRenderer[4];
                _cropRenderers[0] = _spriteRenderer;
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
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
                _spriteBaseLocalPosition = _spriteRenderer.transform.localPosition;
            }

            float halfHorizontal = _cropHorizontalSpacing * 0.5f;
            float halfVertical = _cropVerticalSpacing * 0.5f;
            Vector3[] offsets =
            {
                new Vector3(0f,              halfVertical, 0f), // Back
                new Vector3(-halfHorizontal, 0f,           0f), // Left
                new Vector3( halfHorizontal, 0f,           0f), // Right
                new Vector3(0f,             -halfVertical, 0f)  // Front
            };

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                if (_cropRenderers[i] == null)
                {
                    _cropRenderers[i] = Instantiate(_spriteRenderer, _spriteRenderer.transform.parent);
                    _cropRenderers[i].name = $"CropSprite_{i + 1}";
                }

                _cropRenderers[i].transform.localPosition = _spriteBaseLocalPosition + offsets[i];
                _cropRenderers[i].transform.localRotation = Quaternion.identity;
                _cropRenderers[i].sortingOrder = _cropSortingOrder + (i == 3 ? 1 : 0);
            }
        }

        private void FaceCamera()
        {
            if (_camera == null) return;

            // Match MapPreviewView: keep the visual upright on XY and only turn it
            // horizontally toward the camera.
            Vector3 direction = Vector3.ProjectOnPlane(-_camera.transform.forward, Vector3.up);
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
