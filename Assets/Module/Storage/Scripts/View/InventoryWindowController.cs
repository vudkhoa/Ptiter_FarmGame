using System;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using DG.Tweening;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace Core.Module.Storage.View
{
    [DisallowMultipleComponent]
    public sealed class InventoryWindowController :
        WindowController,
        IOnBeforeWindowOpen,
        IOnWindowClosed
    {
        private const int ItemsPerPage = 12;
        private const float TabRevealDistance = 140f;

        [Header("Data")]
        [SerializeField] private InventoryCatalogSO _catalog;

        [Header("Navigation")]
        [SerializeField] private Button _allTab;
        [SerializeField] private Button _farmTab;
        [FormerlySerializedAs("_decorationTab")]
        [SerializeField] private Button _foodTab;
        [SerializeField] private Button _previousPage;
        [SerializeField] private Button _nextPage;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _pageLabel;

        [Header("Items")]
        [SerializeField] private InventoryItemView[] _itemSlots;
        [SerializeField] private RectTransform _itemsContent;

        [Header("Details")]
        [SerializeField] private GameObject _emptyDetails;
        [SerializeField] private GameObject _selectedDetails;
        [SerializeField] private Image _preview;
        [SerializeField] private TMP_Text _itemName;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private GameObject _actionButtonVisual;

        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<InventoryItemDefinition> _visibleItems = new();
        private IStorageService _storage;
        private InventoryCategory _category = InventoryCategory.All;
        private InventoryItemDefinition _selected;
        private int _pageIndex;
        private InventoryTabMotion _allTabMotion;
        private InventoryTabMotion _farmTabMotion;
        private InventoryTabMotion _foodTabMotion;
        private CanvasGroup _itemsCanvasGroup;
        private Sequence _itemsTransition;
        private Tween _detailsTransition;
        private Vector2 _itemsRestingPosition;
        private bool _motionInitialized;
        private bool _tabMotionInitialized;
        private bool _constructed;

        [Inject]
        public void Construct(
            IStorageService storage,
            ISubscriber<InventoryChangedPayload> inventoryChanged)
        {
            if (_constructed) return;
            _constructed = true;
            _storage = storage;
            _subscriptions.Add(inventoryChanged.Subscribe(OnInventoryChanged));
            Render();
        }

        public void OnBeforeWindowOpen()
        {
            GameplayInputBlockRegistry.Add(this);
            _category = InventoryCategory.All;
            _pageIndex = 0;
            _selected = null;
            RegisterButtons();
            EnsureTabMotionInitialized();
            EnsureMotionInitialized();
            UpdateTabMotion(false);
            Render();
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
            KillMotion();
        }

        private void RegisterButtons()
        {
            UnregisterButtons();
            _allTab?.onClick.AddListener(ShowAll);
            _farmTab?.onClick.AddListener(ShowFarm);
            _foodTab?.onClick.AddListener(ShowFood);
            _previousPage?.onClick.AddListener(PreviousPage);
            _nextPage?.onClick.AddListener(NextPage);
            _closeButton?.onClick.AddListener(Close);
        }

        private void UnregisterButtons()
        {
            _allTab?.onClick.RemoveListener(ShowAll);
            _farmTab?.onClick.RemoveListener(ShowFarm);
            _foodTab?.onClick.RemoveListener(ShowFood);
            _previousPage?.onClick.RemoveListener(PreviousPage);
            _nextPage?.onClick.RemoveListener(NextPage);
            _closeButton?.onClick.RemoveListener(Close);
        }

        private void ShowAll() => SetCategory(InventoryCategory.All);
        private void ShowFarm() => SetCategory(InventoryCategory.FarmProduce);
        private void ShowFood() => SetCategory(InventoryCategory.Food);

        private void SetCategory(InventoryCategory category)
        {
            if (_category == category) return;

            int direction = GetCategoryOrder(category) >
                            GetCategoryOrder(_category)
                ? 1
                : -1;
            _category = category;
            _pageIndex = 0;
            _selected = null;
            UpdateTabMotion(true);
            PlayItemsTransition(direction);
        }

        private void EnsureTabMotionInitialized()
        {
            if (_tabMotionInitialized ||
                _allTab == null ||
                _farmTab == null ||
                _foodTab == null)
                return;

            RectTransform allRect = _allTab.transform as RectTransform;
            RectTransform farmRect = _farmTab.transform as RectTransform;
            if (allRect == null || farmRect == null) return;

            float collapsedX = farmRect.anchoredPosition.x;
            float expandedX = collapsedX - TabRevealDistance;

            _allTabMotion = GetOrAddTabMotion(_allTab);
            _farmTabMotion = GetOrAddTabMotion(_farmTab);
            _foodTabMotion = GetOrAddTabMotion(_foodTab);

            _allTabMotion.Initialize(
                expandedX, collapsedX, 0.15f, DG.Tweening.Ease.OutQuad);
            _farmTabMotion.Initialize(
                expandedX, collapsedX, 0.15f, DG.Tweening.Ease.OutQuad);
            _foodTabMotion.Initialize(
                expandedX, collapsedX, 0.15f, DG.Tweening.Ease.OutQuad);
            _tabMotionInitialized = true;
        }

        private static InventoryTabMotion GetOrAddTabMotion(Button button)
        {
            InventoryTabMotion motion =
                button.GetComponent<InventoryTabMotion>();
            return motion != null
                ? motion
                : button.gameObject.AddComponent<InventoryTabMotion>();
        }

        private void UpdateTabMotion(bool animate)
        {
            EnsureTabMotionInitialized();
            if (!_tabMotionInitialized) return;

            _allTabMotion.SetActive(
                _category == InventoryCategory.All, animate);
            _farmTabMotion.SetActive(
                _category == InventoryCategory.FarmProduce, animate);
            _foodTabMotion.SetActive(
                _category == InventoryCategory.Food, animate);
        }

        private void PreviousPage()
        {
            int targetPage = Mathf.Max(0, _pageIndex - 1);
            if (targetPage == _pageIndex) return;
            _pageIndex = targetPage;
            _selected = null;
            PlayItemsTransition(-1);
        }

        private void NextPage()
        {
            int pageCount = Mathf.Max(1,
                Mathf.CeilToInt(_visibleItems.Count / (float)ItemsPerPage));
            int targetPage = Mathf.Min(pageCount - 1, _pageIndex + 1);
            if (targetPage == _pageIndex) return;
            _pageIndex = targetPage;
            _selected = null;
            PlayItemsTransition(1);
        }

        private void OnInventoryChanged(InventoryChangedPayload payload)
        {
            if (_selected != null && _selected.itemId == payload.ItemId &&
                payload.NewAmount <= 0)
                _selected = null;
            Render();
        }

        private void SelectItem(InventoryItemDefinition definition)
        {
            if (_selected == definition) return;
            _selected = definition;
            Render();
            PlayDetailsFeedback();
        }

        private void EnsureMotionInitialized()
        {
            if (_motionInitialized) return;

            if (_itemsContent == null)
            {
                _itemsContent = transform.Find(
                    "Motion Root/Inventory Panel/Items Content")
                    as RectTransform;
            }

            if (_itemsContent == null) return;

            _itemsCanvasGroup = _itemsContent.GetComponent<CanvasGroup>();
            if (_itemsCanvasGroup == null)
            {
                _itemsCanvasGroup =
                    _itemsContent.gameObject.AddComponent<CanvasGroup>();
            }

            _itemsRestingPosition = _itemsContent.anchoredPosition;
            _motionInitialized = true;
        }

        private void PlayItemsTransition(int direction)
        {
            EnsureMotionInitialized();
            if (!_motionInitialized || !gameObject.activeInHierarchy)
            {
                Render();
                return;
            }

            _itemsTransition?.Kill(false);
            _itemsContent.anchoredPosition = _itemsRestingPosition;
            _itemsCanvasGroup.alpha = 1f;

            float offset = 18f * Mathf.Sign(direction);
            _itemsTransition = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _itemsTransition.Join(
                TweenAnchoredPosition(
                        _itemsContent,
                        _itemsRestingPosition - Vector2.right * offset,
                        0.07f)
                    .SetEase(Ease.InQuad));
            _itemsTransition.Join(
                TweenCanvasGroupAlpha(_itemsCanvasGroup, 0f, 0.07f)
                    .SetEase(Ease.InQuad));
            _itemsTransition.AppendCallback(() =>
            {
                Render();
                _itemsContent.anchoredPosition =
                    _itemsRestingPosition + Vector2.right * offset;
            });
            _itemsTransition.Append(
                TweenAnchoredPosition(
                        _itemsContent,
                        _itemsRestingPosition,
                        0.10f)
                    .SetEase(Ease.OutCubic));
            _itemsTransition.Join(
                TweenCanvasGroupAlpha(_itemsCanvasGroup, 1f, 0.10f)
                    .SetEase(Ease.OutQuad));
            _itemsTransition.OnComplete(() => _itemsTransition = null);
        }

        private void PlayDetailsFeedback()
        {
            if (_selectedDetails == null ||
                !_selectedDetails.activeInHierarchy)
                return;

            RectTransform detailsRect =
                _selectedDetails.transform as RectTransform;
            if (detailsRect == null) return;

            CanvasGroup group =
                _selectedDetails.GetComponent<CanvasGroup>();
            if (group == null)
                group = _selectedDetails.AddComponent<CanvasGroup>();

            _detailsTransition?.Kill(false);
            detailsRect.localScale = Vector3.one * 0.92f;
            group.alpha = 0f;

            Sequence feedback = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            feedback.Join(
                detailsRect
                    .DOScale(1f, 0.12f)
                    .SetEase(Ease.OutCubic));
            feedback.Join(
                TweenCanvasGroupAlpha(group, 1f, 0.10f)
                    .SetEase(Ease.OutQuad));
            _detailsTransition = feedback;
        }

        private static int GetCategoryOrder(InventoryCategory category)
        {
            return category switch
            {
                InventoryCategory.All => 0,
                InventoryCategory.FarmProduce => 1,
                InventoryCategory.Food => 2,
                _ => 0
            };
        }

        private static Tween TweenAnchoredPosition(
            RectTransform target,
            Vector2 endValue,
            float duration)
        {
            return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                endValue,
                duration);
        }

        private static Tween TweenCanvasGroupAlpha(
            CanvasGroup target,
            float endValue,
            float duration)
        {
            return DOTween.To(
                () => target.alpha,
                value => target.alpha = value,
                endValue,
                duration);
        }

        private void KillMotion()
        {
            _itemsTransition?.Kill(false);
            _itemsTransition = null;
            _detailsTransition?.Kill(false);
            _detailsTransition = null;

            if (_motionInitialized)
            {
                _itemsContent.anchoredPosition = _itemsRestingPosition;
                _itemsCanvasGroup.alpha = 1f;
            }

            if (_selectedDetails != null)
            {
                _selectedDetails.transform.localScale = Vector3.one;
                CanvasGroup group =
                    _selectedDetails.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
            }
        }

        private void Render()
        {
            if (_storage == null || _catalog == null) return;

            _visibleItems.Clear();
            IReadOnlyList<InventoryItemDefinition> definitions = _catalog.Items;
            for (int i = 0; i < definitions.Count; i++)
            {
                InventoryItemDefinition item = definitions[i];
                if (item == null || string.IsNullOrWhiteSpace(item.itemId)) continue;
                if (_category != InventoryCategory.All && item.category != _category)
                    continue;
                if (_storage.GetItemCount(item.itemId) > 0)
                    _visibleItems.Add(item);
            }

            int pageCount = Mathf.Max(1,
                Mathf.CeilToInt(_visibleItems.Count / (float)ItemsPerPage));
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pageCount - 1);
            if (_pageLabel != null)
                _pageLabel.text = $"{_pageIndex + 1} / {pageCount}";

            for (int slot = 0; slot < (_itemSlots?.Length ?? 0); slot++)
            {
                int index = _pageIndex * ItemsPerPage + slot;
                InventoryItemDefinition item = index < _visibleItems.Count
                    ? _visibleItems[index]
                    : null;
                int amount = item == null ? 0 : _storage.GetItemCount(item.itemId);
                _itemSlots[slot]?.Bind(item, amount, item == _selected, SelectItem);
            }

            if (_previousPage != null) _previousPage.interactable = _pageIndex > 0;
            if (_nextPage != null) _nextPage.interactable = _pageIndex + 1 < pageCount;
            RenderDetails();
        }

        private void RenderDetails()
        {
            bool hasSelection = _selected != null;
            _emptyDetails?.SetActive(!hasSelection);
            _selectedDetails?.SetActive(hasSelection);
            if (!hasSelection) return;

            if (_preview != null)
            {
                _preview.sprite = _selected.preview != null
                    ? _selected.preview
                    : _selected.icon;
                _preview.enabled = _preview.sprite != null;
            }
            if (_itemName != null) _itemName.text = _selected.displayName;
            if (_description != null) _description.text = _selected.description;

            // Reserved artwork only. Selling will be implemented in a later task.
            _actionButtonVisual?.SetActive(true);
        }

        protected override void OnDestroy()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
            KillMotion();
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            base.OnDestroy();
        }
    }
}
