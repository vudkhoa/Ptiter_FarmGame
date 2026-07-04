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

        [Header("Billboard Settings")]
        [SerializeField] private bool _useBillboard = true;

        public void UpdateView(FarmSlotSaveData slot, FarmDatabaseSO database)
        {
            // 1. If slot data is null or completely empty (unplanted Soil / unoccupied Barn)
            if (slot == null || (slot.state == FarmSlotState.Empty && string.IsNullOrEmpty(slot.entityId)))
            {
                if (_spriteRenderer != null) _spriteRenderer.sprite = null;
                if (_progressBar != null) _progressBar.gameObject.SetActive(false);
                if (_feedBubble != null) _feedBubble.SetActive(false);
                if (_harvestBubble != null) _harvestBubble.SetActive(false);
                return;
            }

            // 2. Resolve Slot States
            switch (slot.state)
            {
                case FarmSlotState.Empty:
                    // If it is an adult animal, keep displaying the adult sprite instead of null
                    if (slot.isAnimal && slot.isAdult)
                    {
                        var data = database.GetAnimalById(slot.entityId);
                        if (data != null && data.growthSprites != null && data.growthSprites.Length >= 2)
                        {
                            if (_spriteRenderer != null) _spriteRenderer.sprite = data.growthSprites[2];
                        }
                    }
                    else
                    {
                        if (_spriteRenderer != null) _spriteRenderer.sprite = null;
                    }

                    if (_progressBar != null) _progressBar.gameObject.SetActive(false);
                    if (_harvestBubble != null) _harvestBubble.SetActive(false);

                    if (slot.isAnimal && !slot.isFed)
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

                    // Fetch ScriptableObject config and calculate growth stage
                    Sprite[] growthSprites;
                    float requiredTime;
                    float stage2Threshold;

                    if (slot.isAnimal)
                    {
                        var data = database.GetAnimalById(slot.entityId);
                        if (data == null) return;
                        growthSprites = data.growthSprites;
                        requiredTime = data.productionTime;
                        stage2Threshold = data.stage2Threshold;
                    }
                    else
                    {
                        var data = database.GetCropById(slot.entityId);
                        if (data == null) return;
                        growthSprites = data.growthSprites;
                        requiredTime = data.growTime;
                        stage2Threshold = data.stage2Threshold;
                    }

                    float progress = requiredTime > 0 ? slot.growthTimeSec / requiredTime : 0;
                    progress = Mathf.Clamp01(progress);

                    // Apply Morphing Sprites (Stage 1 vs Stage 2)
                    if (growthSprites != null && growthSprites.Length >= 2)
                    {
                        if (_spriteRenderer != null)
                        {
                            if (slot.isAnimal && slot.isAdult)
                            {
                                // Keep displaying the adult sprite for grown-up animals
                                _spriteRenderer.sprite = growthSprites[2];
                            }
                            else
                            {
                                _spriteRenderer.sprite = progress < stage2Threshold ? growthSprites[0] : growthSprites[1];
                            }
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
                    if (slot.isAnimal)
                    {
                        var data = database.GetAnimalById(slot.entityId);
                        if (data != null && data.growthSprites != null && data.growthSprites.Length >= 3)
                            ripeSprite = data.growthSprites[2];
                    }
                    else
                    {
                        var data = database.GetCropById(slot.entityId);
                        if (data != null && data.growthSprites != null && data.growthSprites.Length >= 3)
                            ripeSprite = data.growthSprites[2];
                    }

                    if (_spriteRenderer != null && ripeSprite != null)
                    {
                        _spriteRenderer.sprite = ripeSprite;
                    }
                    break;
            }
        }

        private void LateUpdate()
        {
            // Rotate the sprite plane parallel to the camera view plane in 3D perspective
            if (_useBillboard && Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
