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
        private Button _cookButton;
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
            rootRect.sizeDelta = new Vector2(1450f, 250f);
            rootRect.anchoredPosition = new Vector2(0f, 55f - index * 315f);

            Image background = root.AddComponent<Image>();
            background.color = new Color(1f, 0.80f, 0.38f, 0.10f);
            background.raycastTarget = false;

            FoodRecipeItemView view = root.AddComponent<FoodRecipeItemView>();
            view.Build(font);
            return view;
        }

        public void Bind(
            FoodRecipeViewData data,
            Sprite lockIcon,
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
            _dishImage.sprite = data.MockSprite;
            _dishImage.enabled = data.MockSprite != null;
            _title.text = data.DisplayName ?? string.Empty;
            _ingredients.text = data.MockIngredients ?? string.Empty;
            _lockImage.sprite = lockIcon;
            _lockImage.enabled = lockIcon != null;
            _unlockCostText.text = $"CẦN {data.StarCost}";
            _unlockStarImage.sprite = starIcon;
            _unlockStarImage.enabled = starIcon != null;
            _unlockSuffixText.text = starIcon != null
                ? "ĐỂ MỞ KHÓA"
                : "SAO ĐỂ MỞ KHÓA";

            _lockButton.onClick.RemoveAllListeners();
            _cookButton.onClick.RemoveAllListeners();
            if (locked)
                _lockButton.onClick.AddListener(() => unlock?.Invoke(data.RecipeId));
            else
                _cookButton.onClick.AddListener(() => cook?.Invoke(data.RecipeId));

            bool animateUnlock = _hasState && _wasLocked && !locked;
            ApplyLockState(locked, animateUnlock);
            _wasLocked = locked;
            _hasState = true;
        }

        private void Build(TMP_FontAsset font)
        {
            GameObject content = CreateUiObject(
                "Obscured Content", transform as RectTransform);
            Stretch(content.GetComponent<RectTransform>());
            _contentGroup = content.AddComponent<CanvasGroup>();

            _dishImage = CreateImage(
                "Dish Silhouette", content.transform, new Vector2(-560f, 0f),
                new Vector2(240f, 210f));
            _dishImage.preserveAspect = true;
            _dishImage.raycastTarget = false;

            _title = CreateText(
                "Recipe Name", content.transform, font,
                new Vector2(-165f, 48f), new Vector2(520f, 72f), 46f,
                FontStyles.Bold, TextAlignmentOptions.Left);
            _title.color = new Color(0.34f, 0.15f, 0.12f, 1f);

            _ingredients = CreateText(
                "Mock Ingredients", content.transform, font,
                new Vector2(-165f, -45f), new Vector2(520f, 90f), 28f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _ingredients.color = new Color(0.38f, 0.20f, 0.14f, 1f);

            _cookButton = CreateButton(
                "Cook Button", content.transform, font,
                new Vector2(565f, 0f), new Vector2(250f, 105f), "NẤU");

            _lockRoot = CreateUiObject(
                "Locked Veil", transform as RectTransform);
            RectTransform veilRect = _lockRoot.GetComponent<RectTransform>();
            Stretch(veilRect);
            Image veil = _lockRoot.AddComponent<Image>();
            veil.color = new Color(0.12f, 0.07f, 0.04f, 0.68f);
            veil.raycastTarget = false;
            _lockGroup = _lockRoot.AddComponent<CanvasGroup>();

            GameObject lockObject = CreateUiObject(
                "Unlock", veilRect);
            RectTransform lockRect = lockObject.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(110f, 110f);
            lockRect.anchoredPosition = new Vector2(0f, 34f);
            _lockImage = lockObject.AddComponent<Image>();
            _lockImage.preserveAspect = true;
            _lockButton = lockObject.AddComponent<Button>();
            _lockButton.targetGraphic = _lockImage;

            _unlockCostText = CreateText(
                "Unlock Cost", _lockRoot.transform, font,
                new Vector2(-125f, -72f), new Vector2(130f, 48f), 27f,
                FontStyles.Bold, TextAlignmentOptions.Right);
            _unlockCostText.color = Color.white;

            _unlockStarImage = CreateImage(
                "Unlock Star", _lockRoot.transform,
                new Vector2(-38f, -72f), new Vector2(36f, 36f));
            _unlockStarImage.preserveAspect = true;
            _unlockStarImage.raycastTarget = false;

            _unlockSuffixText = CreateText(
                "Unlock Suffix", _lockRoot.transform, font,
                new Vector2(84f, -72f), new Vector2(200f, 48f), 27f,
                FontStyles.Bold, TextAlignmentOptions.Left);
            _unlockSuffixText.color = Color.white;
        }

        private void ApplyLockState(bool locked, bool animateUnlock)
        {
            _contentGroup.DOKill(false);
            _lockGroup.DOKill(false);
            _lockRoot.transform.DOKill(false);

            _contentGroup.gameObject.SetActive(!locked || animateUnlock);
            _title.gameObject.SetActive(!locked);
            _ingredients.gameObject.SetActive(!locked);
            _cookButton.gameObject.SetActive(!locked);

            if (!animateUnlock)
            {
                _contentGroup.alpha = locked ? 0.16f : 1f;
                _lockGroup.alpha = 1f;
                _lockRoot.transform.localScale = Vector3.one;
                _lockRoot.SetActive(locked);
                return;
            }

            _contentGroup.alpha = 0.16f;
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
            string label)
        {
            GameObject buttonObject = CreateUiObject(name, parent as RectTransform);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.10f, 0.53f, 0.17f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

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
