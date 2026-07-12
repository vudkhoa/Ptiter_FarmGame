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
                        if (entity.growthSprites != null && entity.growthSprites.Length >= 3)
                        {
                            if (_spriteRenderer != null) _spriteRenderer.sprite = entity.growthSprites[2];
                        }
                    }
                    else
                    {
                        if (_spriteRenderer != null) _spriteRenderer.sprite = null;
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
                    if (growthSprites != null && growthSprites.Length >= 2)
                    {
                        if (_spriteRenderer != null)
                        {
                            if (isAnimal && slot.isAdult)
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
                    if (entity.growthSprites != null && entity.growthSprites.Length >= 3)
                    {
                        ripeSprite = entity.growthSprites[2];
                    }

                    if (_spriteRenderer != null && ripeSprite != null)
                    {
                        _spriteRenderer.sprite = ripeSprite;
                    }
                    break;
            }
        }
    }
}
