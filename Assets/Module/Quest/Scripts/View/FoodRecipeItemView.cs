using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Quest
{
    [DisallowMultipleComponent]
    public sealed class FoodRecipeItemView : MonoBehaviour
    {
        [Header("Recipe content")]
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private Image _dishImage;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private GameObject _ingredientRowOne;
        [SerializeField] private TMP_Text _ingredientLineOne;
        [SerializeField] private GameObject _ingredientRowTwo;
        [SerializeField] private TMP_Text _ingredientLineTwo;
        [SerializeField] private TMP_Text _story;
        [SerializeField] private Button _cookButton;
        [SerializeField] private Image _cookButtonImage;

        [Header("Locked state")]
        [SerializeField] private GameObject _lockRoot;
        [SerializeField] private CanvasGroup _lockGroup;
        [SerializeField] private Button _lockButton;
        [SerializeField] private Image _lockImage;
        [SerializeField] private GameObject _unlockCostRoot;
        [SerializeField] private TMP_Text _unlockCostText;
        [SerializeField] private Image _unlockStarImage;
        [SerializeField] private TMP_Text _unlockSuffixText;
        [SerializeField] private GameObject _developmentRoot;
        [SerializeField] private Image _developmentLockImage;
        [SerializeField] private TMP_Text _developmentText;

        private bool _hasState;
        private bool _wasLocked;
        private bool _wasInDevelopment;

        public void Bind(
            FoodRecipeViewData data,
            Sprite lockIcon,
            Sprite cookButtonSprite,
            Sprite starIcon,
            Action<string> unlock,
            Action<string> cook)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            bool locked = data.AccessState != FoodRecipeAccessState.Unlocked;
            bool inDevelopment =
                data.AccessState == FoodRecipeAccessState.InDevelopment;

            if (_dishImage != null)
            {
                _dishImage.sprite = data.MockSprite;
                _dishImage.enabled = data.MockSprite != null;
            }

            if (_title != null) _title.text = data.DisplayName ?? string.Empty;
            BindIngredients(data.MockIngredients);
            if (_story != null) _story.text = data.Story ?? string.Empty;

            if (_cookButtonImage != null)
            {
                _cookButtonImage.sprite = cookButtonSprite;
                _cookButtonImage.enabled = cookButtonSprite != null;
            }

            if (_lockImage != null)
            {
                _lockImage.sprite = lockIcon;
                _lockImage.enabled = lockIcon != null;
            }

            if (_developmentLockImage != null)
            {
                _developmentLockImage.sprite = lockIcon;
                _developmentLockImage.enabled = lockIcon != null;
            }

            RenderUnlockMessage(data.StarCost, starIcon, inDevelopment);
            RegisterActions(data.RecipeId, locked, inDevelopment, unlock, cook);

            bool animateUnlock =
                _hasState && _wasLocked && !_wasInDevelopment && !locked;
            ApplyLockState(locked, inDevelopment, animateUnlock);
            _wasLocked = locked;
            _wasInDevelopment = inDevelopment;
            _hasState = true;
        }

        private void BindIngredients(string ingredients)
        {
            string[] lines = (ingredients ?? string.Empty).Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            string first = lines.Length > 0 ? lines[0].Trim() : string.Empty;
            string second = lines.Length > 1 ? lines[1].Trim() : string.Empty;

            if (_ingredientLineOne != null) _ingredientLineOne.text = first;
            if (_ingredientLineTwo != null) _ingredientLineTwo.text = second;
            if (_ingredientRowOne != null)
                _ingredientRowOne.SetActive(!string.IsNullOrEmpty(first));
            if (_ingredientRowTwo != null)
                _ingredientRowTwo.SetActive(!string.IsNullOrEmpty(second));
        }

        private void RegisterActions(
            string recipeId,
            bool locked,
            bool inDevelopment,
            Action<string> unlock,
            Action<string> cook)
        {
            if (_lockButton != null)
            {
                _lockButton.onClick.RemoveAllListeners();
                _lockButton.interactable = locked && !inDevelopment;
                if (locked && !inDevelopment)
                    _lockButton.onClick.AddListener(
                        () => unlock?.Invoke(recipeId));
            }

            if (_cookButton != null)
            {
                _cookButton.onClick.RemoveAllListeners();
                _cookButton.interactable = !locked;
                if (!locked)
                    _cookButton.onClick.AddListener(
                        () => cook?.Invoke(recipeId));
            }
        }

        private void RenderUnlockMessage(
            int starCost,
            Sprite starIcon,
            bool inDevelopment)
        {
            if (_unlockCostRoot != null)
                _unlockCostRoot.SetActive(!inDevelopment);
            if (_developmentRoot != null)
                _developmentRoot.SetActive(inDevelopment);
            if (_developmentText != null)
                _developmentText.text = "ĐANG PHÁT TRIỂN";

            if (_unlockCostText != null)
                _unlockCostText.text = $"CẦN {starCost}";
            if (_unlockStarImage != null)
            {
                _unlockStarImage.sprite = starIcon;
                _unlockStarImage.enabled = starIcon != null;
            }
            if (_unlockSuffixText != null)
                _unlockSuffixText.text = starIcon != null
                    ? "ĐỂ MỞ KHÓA"
                    : "SAO ĐỂ MỞ KHÓA";
        }

        private void ApplyLockState(
            bool locked,
            bool inDevelopment,
            bool animateUnlock)
        {
            _contentGroup?.DOKill(false);
            _lockGroup?.DOKill(false);
            if (_lockRoot != null) _lockRoot.transform.DOKill(false);

            SetActive(_title, !locked);
            SetActive(_ingredientRowOne, !locked);
            SetActive(_ingredientRowTwo, !locked);
            SetActive(_story, !locked);
            SetActive(_cookButton, !locked);

            if (_developmentRoot != null)
                _developmentRoot.SetActive(inDevelopment);

            if (!animateUnlock)
            {
                if (_contentGroup != null)
                    _contentGroup.alpha =
                        inDevelopment ? 0f : locked ? 0.38f : 1f;
                if (_lockGroup != null) _lockGroup.alpha = 1f;
                if (_lockRoot != null)
                {
                    _lockRoot.transform.localScale = Vector3.one;
                    _lockRoot.SetActive(locked && !inDevelopment);
                }
                return;
            }

            if (_developmentRoot != null)
                _developmentRoot.SetActive(false);

            if (_contentGroup == null || _lockGroup == null || _lockRoot == null)
            {
                if (_lockRoot != null) _lockRoot.SetActive(false);
                if (_contentGroup != null) _contentGroup.alpha = 1f;
                return;
            }

            _contentGroup.alpha = 0.38f;
            _lockGroup.alpha = 1f;
            _lockRoot.SetActive(true);
            _lockRoot.transform.localScale = Vector3.one;
            DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Join(_lockGroup.DOFade(0f, 0.20f))
                .Join(_lockRoot.transform.DOScale(1.12f, 0.20f))
                .AppendCallback(() => _lockRoot.SetActive(false))
                .Join(_contentGroup.DOFade(1f, 0.22f));
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
