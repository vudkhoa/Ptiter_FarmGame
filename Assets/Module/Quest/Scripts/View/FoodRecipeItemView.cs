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
        private CanvasGroup _contentGroup;
        private Image _dishImage;
        private TMP_Text _title;
        private TMP_Text _ingredients;
        private TMP_Text _story;
        private Button _cookButton;
        private Image _cookButtonImage;
        private Button _lockButton;
        private Image _lockImage;
        private TMP_Text _unlockCostText;
        private Image _unlockStarImage;
        private TMP_Text _unlockSuffixText;
        private GameObject _lockRoot;
        private CanvasGroup _lockGroup;
        private bool _hasState;
        private bool _wasLocked;

        public static FoodRecipeItemView Create(
            RectTransform parent,
            TMP_FontAsset font,
            int index)
        {
            GameObject root = CreateUiObject(
                $"Food Recipe {index + 1}", parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1450f, 330f);
            rootRect.anchoredPosition = new Vector2(0f, 90f - index * 375f);

            Image background = root.AddComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;

            FoodRecipeItemView view = root.AddComponent<FoodRecipeItemView>();
            view.Build(font);
            return view;
        }

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
            _dishImage.sprite = data.MockSprite;
            _dishImage.enabled = data.MockSprite != null;
            _title.text = data.DisplayName ?? string.Empty;
            _ingredients.text = data.MockIngredients ?? string.Empty;
            _story.text = data.Story ?? string.Empty;
            _cookButtonImage.sprite = cookButtonSprite;
            _cookButtonImage.enabled = cookButtonSprite != null;
            _lockImage.sprite = lockIcon;
            _lockImage.enabled = lockIcon != null;
            RenderUnlockMessage(data.StarCost, starIcon, inDevelopment);

            _lockButton.onClick.RemoveAllListeners();
            _cookButton.onClick.RemoveAllListeners();
            _lockButton.interactable = locked && !inDevelopment;
            if (locked && !inDevelopment)
                _lockButton.onClick.AddListener(() => unlock?.Invoke(data.RecipeId));
            else if (!locked)
                _cookButton.onClick.AddListener(() => cook?.Invoke(data.RecipeId));

            bool animateUnlock = _hasState && _wasLocked && !locked;
            ApplyLockState(locked, animateUnlock);
            _wasLocked = locked;
            _hasState = true;
        }

        private void RenderUnlockMessage(
            int starCost,
            Sprite starIcon,
            bool inDevelopment)
        {
            RectTransform costRect = _unlockCostText.rectTransform;
            if (inDevelopment)
            {
                _unlockCostText.text = "ĐANG PHÁT TRIỂN";
                _unlockCostText.alignment = TextAlignmentOptions.Center;
                costRect.anchoredPosition = new Vector2(0f, -55f);
                costRect.sizeDelta = new Vector2(340f, 52f);
                _unlockStarImage.enabled = false;
                _unlockSuffixText.text = string.Empty;
                return;
            }

            _unlockCostText.text = $"CẦN {starCost}";
            _unlockCostText.alignment = TextAlignmentOptions.Right;
            costRect.anchoredPosition = new Vector2(-105f, -55f);
            costRect.sizeDelta = new Vector2(100f, 48f);
            _unlockStarImage.sprite = starIcon;
            _unlockStarImage.enabled = starIcon != null;
            _unlockSuffixText.text = starIcon != null
                ? "ĐỂ MỞ KHÓA"
                : "SAO ĐỂ MỞ KHÓA";
        }

        private void Build(TMP_FontAsset font)
        {
            GameObject content = CreateUiObject(
                "Obscured Content", transform as RectTransform);
            Stretch(content.GetComponent<RectTransform>());
            _contentGroup = content.AddComponent<CanvasGroup>();

            _dishImage = CreateImage(
                "Dish Silhouette", content.transform, new Vector2(-560f, 40f),
                new Vector2(240f, 210f));
            _dishImage.preserveAspect = true;
            _dishImage.raycastTarget = false;

            _title = CreateText(
                "Recipe Name", content.transform, font,
                new Vector2(-165f, 92f), new Vector2(520f, 72f), 46f,
                FontStyles.Bold, TextAlignmentOptions.Left);
            _title.color = new Color(0.34f, 0.15f, 0.12f, 1f);

            _ingredients = CreateText(
                "Mock Ingredients", content.transform, font,
                new Vector2(-165f, 8f), new Vector2(520f, 100f), 30f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _ingredients.color = new Color(0.38f, 0.20f, 0.14f, 1f);

            _story = CreateText(
                "Story", content.transform, font,
                new Vector2(0f, -88f), new Vector2(1320f, 134f), 32f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _story.lineSpacing = 3f;
            _story.color = new Color(0.38f, 0.20f, 0.14f, 1f);

            _cookButton = CreateButton(
                "Cook Button", content.transform, font,
                new Vector2(565f, 40f), new Vector2(307f, 138f), "NẤU",
                out _cookButtonImage);

            _lockRoot = CreateUiObject(
                "Locked Veil", transform as RectTransform);
            RectTransform veilRect = _lockRoot.GetComponent<RectTransform>();
            veilRect.anchorMin = veilRect.anchorMax = new Vector2(0.5f, 0.5f);
            veilRect.pivot = new Vector2(0.5f, 0.5f);
            veilRect.sizeDelta = new Vector2(360f, 210f);
            veilRect.anchoredPosition = new Vector2(565f, 30f);
            Image veil = _lockRoot.AddComponent<Image>();
            veil.color = Color.clear;
            veil.raycastTarget = true;
            _lockButton = _lockRoot.AddComponent<Button>();
            _lockButton.targetGraphic = veil;
            _lockButton.navigation =
                new Navigation { mode = Navigation.Mode.None };
            _lockGroup = _lockRoot.AddComponent<CanvasGroup>();

            GameObject lockObject = CreateUiObject(
                "Unlock", veilRect);
            RectTransform lockRect = lockObject.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(100f, 100f);
            lockRect.anchoredPosition = new Vector2(0f, 35f);
            _lockImage = lockObject.AddComponent<Image>();
            _lockImage.preserveAspect = true;
            _lockImage.raycastTarget = false;

            _unlockCostText = CreateText(
                "Unlock Cost", _lockRoot.transform, font,
                new Vector2(-105f, -55f), new Vector2(100f, 48f), 30f,
                FontStyles.Bold, TextAlignmentOptions.Right);
            _unlockCostText.color = new Color(0.34f, 0.15f, 0.12f, 1f);

            _unlockStarImage = CreateImage(
                "Unlock Star", _lockRoot.transform,
                new Vector2(-38f, -55f), new Vector2(36f, 36f));
            _unlockStarImage.preserveAspect = true;
            _unlockStarImage.raycastTarget = false;

            _unlockSuffixText = CreateText(
                "Unlock Suffix", _lockRoot.transform, font,
                new Vector2(84f, -55f), new Vector2(200f, 48f), 30f,
                FontStyles.Bold, TextAlignmentOptions.Left);
            _unlockSuffixText.color = new Color(0.34f, 0.15f, 0.12f, 1f);
        }

        private void ApplyLockState(bool locked, bool animateUnlock)
        {
            _contentGroup.DOKill(false);
            _lockGroup.DOKill(false);
            _lockRoot.transform.DOKill(false);

            _contentGroup.gameObject.SetActive(true);
            _title.gameObject.SetActive(!locked);
            _ingredients.gameObject.SetActive(!locked);
            _story.gameObject.SetActive(!locked);
            _cookButton.gameObject.SetActive(!locked);

            if (!animateUnlock)
            {
                _contentGroup.alpha = locked ? 0.38f : 1f;
                _lockGroup.alpha = 1f;
                _lockRoot.transform.localScale = Vector3.one;
                _lockRoot.SetActive(locked);
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

        private static Button CreateButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size,
            string label,
            out Image image)
        {
            GameObject buttonObject = CreateUiObject(name, parent as RectTransform);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            image = buttonObject.AddComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            TMP_Text text = CreateText(
                "Label", buttonObject.transform, font, Vector2.zero, size,
                38f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.text = label;
            text.color = Color.white;
            return button;
        }

        private static Image CreateImage(
            string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject child = CreateUiObject(name, parent as RectTransform);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return child.AddComponent<Image>();
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject child = CreateUiObject(name, parent as RectTransform);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(
            string name, RectTransform parent)
        {
            GameObject child = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
