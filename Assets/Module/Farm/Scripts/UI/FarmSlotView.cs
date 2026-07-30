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
        [SerializeField] private Transform _uiRoot;
        [SerializeField] private bool _useBillboard = true;
        [SerializeField, Min(0f)] private float _cropXSpacing = 0.4f;
        [SerializeField, Min(0f)] private float _cropZSpacing = 0.4f;
        [SerializeField] private int _cropSortingOrder = 1;
        [SerializeField] private int _animalSortingOrder = 1;

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
            if (_useBillboard)
            {
                if (_spriteRenderer != null) MatchCameraRotation(_spriteRenderer.transform);
                if (_uiRoot != null) MatchCameraRotation(_uiRoot);
            }
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

            float halfX = _cropXSpacing * 0.5f;
            float halfZ = _cropZSpacing * 0.5f;
            Vector3[] offsets =
            {
                new Vector3(-halfX, 0f, -halfZ),
                new Vector3( halfX, 0f, -halfZ),
                new Vector3(-halfX, 0f,  halfZ),
                new Vector3( halfX, 0f,  halfZ)
            };

            for (int i = 0; i < _cropRenderers.Length; i++)
            {
                if (_cropRenderers[i] == null)
                {
                    _cropRenderers[i] = Instantiate(_spriteRenderer, _spriteRenderer.transform.parent);
                    _cropRenderers[i].name = $"CropSprite_{i + 1}";
                }

                _cropRenderers[i].transform.localPosition = _spriteBaseLocalPosition + offsets[i];
                if (_useBillboard) MatchCameraRotation(_cropRenderers[i].transform);
                _cropRenderers[i].sortingOrder = _cropSortingOrder;
            }
        }

        private void MatchCameraRotation(Transform target)
        {
            if (_camera == null || target == null) return;
            target.rotation = _camera.transform.rotation;
        }
    }
}
