using System;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
using Core.Module.Input;
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
            EnsureModalInputBlocker();
            GameplayInputBlockRegistry.Add(this);
            _category = InventoryCategory.All;
            _pageIndex = 0;
            _selected = null;
            RegisterButtons();
            EnsureTabMotionInitialized();
            UpdateTabMotion(false);
            Render();
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
            UnregisterButtons();
        }

        private void EnsureModalInputBlocker()
        {
            const string blockerName = "Modal Input Blocker";
            Transform existing = transform.Find(blockerName);
            GameObject blocker = existing != null
                ? existing.gameObject
                : new GameObject(
                    blockerName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            RectTransform rect = blocker.transform as RectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(10000f, 10000f);
            rect.SetAsFirstSibling();

            Image image = blocker.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            image.maskable = false;
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
            _category = category;
            _pageIndex = 0;
            _selected = null;
            UpdateTabMotion(true);
            Render();
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
            _pageIndex = Mathf.Max(0, _pageIndex - 1);
            _selected = null;
            Render();
        }

        private void NextPage()
        {
            int pageCount = Mathf.Max(1,
                Mathf.CeilToInt(_visibleItems.Count / (float)ItemsPerPage));
            _pageIndex = Mathf.Min(pageCount - 1, _pageIndex + 1);
            _selected = null;
            Render();
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
            _selected = definition;
            Render();
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
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();
            base.OnDestroy();
        }
    }
}
