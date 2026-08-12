using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Module.Storage;
using Core.Module.Farm;
using Core.Module.Toast;
using Core.Module.Tutorial;
using VContainer;
using BrunoMikoski.UIManager;

namespace MyOwn.ServiceHarness
{
    /// UI chọn hạt giống/con non, biên dịch trong Assembly-CSharp. Được VContainer inject qua
    /// autoInjectGameObjects của Test_UIManager.
    [DisallowMultipleComponent]
    public sealed class FarmSeedSelectorUI : WindowController, IOnBeforeWindowOpen, IOnWindowClosed
    {
        [Header("UI Containers")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private FarmSeedSelectorItemView _itemTemplate;
        [SerializeField] private Button _closeButton;

        [Tooltip("{0} is the price in coins.")]
        [SerializeField] private string _insufficientCoinsFormat = "Không đủ tiền! Cần {0} vàng";

        // Cached once: a method group passed per row would allocate a delegate for every button.
        private Action<FarmEntityData> _onEntitySelected;

        private IFarmService _farmService;
        private IStorageService _storageService;
        private FarmDatabaseSO _database;
        private OpenFarmSelectorUIPayload _currentContext;
        private Transform _tutorialSeedAnchor;

        #region Public API
        [Inject]
        public void Construct(
            IFarmService farmService,
            IStorageService storageService,
            FarmDatabaseSO database)
        {
            _farmService = farmService;
            _storageService = storageService;
            _database = database;
        }

        public void OnBeforeWindowOpen()
        {
            if (_itemTemplate != null) _itemTemplate.gameObject.SetActive(false);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        public void OnWindowClosed()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);

            ClearTutorialAnchor();
        }

        /// Được gọi bởi Bridge ngay sau khi UIManager mở cửa sổ và lấy được Instance.
        public void InitializeSelector(OpenFarmSelectorUIPayload context)
        {
            _currentContext = context;
            PopulateUI();
        }
        #endregion

        #region Private Methods
        private void PopulateUI()
        {
            if (_itemContainer == null || _itemTemplate == null || _database == null) return;

            _itemContainer.gameObject.SetActive(true);

            ClearSpawnedItems();
            ClearTutorialAnchor();

            IReadOnlyList<FarmEntityData> entities = _database.AllEntities;
            if (entities == null) return;

            // Indexed, not foreach: AllEntities is an IReadOnlyList, whose enumerator boxes.
            for (int i = 0; i < entities.Count; i++)
            {
                FarmEntityData entity = entities[i];
                if (entity == null) continue;

                bool isAnimal = entity.entityType == FarmEntityType.Animal;
                if (isAnimal != _currentContext.IsAnimal) continue;

                CreateItemButton(entity);
            }
        }

        // Backwards over childCount rather than foreach: Transform's enumerator is non-generic
        // (it boxes), and removing while walking forward skips entries.
        private void ClearSpawnedItems()
        {
            Transform template = _itemTemplate.transform;

            for (int i = _itemContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = _itemContainer.GetChild(i);
                if (child == template) continue;

                Destroy(child.gameObject);
            }
        }

        private void CreateItemButton(FarmEntityData entity)
        {
            FarmSeedSelectorItemView itemView = Instantiate(_itemTemplate, _itemContainer);
            itemView.gameObject.SetActive(true);

            bool canAfford = _storageService.Coins >= entity.coinCost;
            _onEntitySelected ??= OnEntitySelected;
            itemView.Bind(entity, canAfford, _onEntitySelected);

            TryMarkTutorialSeed(itemView, canAfford);
        }

        /// <summary>
        /// Affordability is re-read here rather than reused from populate time: the balance can
        /// move while the panel is open, and the row the player taps must reflect that.
        /// </summary>
        private void OnEntitySelected(FarmEntityData entity)
        {
            if (entity == null) return;

            if (_storageService.Coins < entity.coinCost)
            {
                ToastHub.Show(
                    string.Format(_insufficientCoinsFormat, entity.coinCost),
                    ToastStyle.Error);
                return;
            }

            if (_farmService.TryPlant(_currentContext.Cell, entity.EntityId)) Close();
        }

        /// Points the "choose a seed" step at the first row the player can actually buy - the hand
        /// must never sit on a greyed-out button.
        private void TryMarkTutorialSeed(FarmSeedSelectorItemView itemView, bool canAfford)
        {
            if (_tutorialSeedAnchor != null || !canAfford || _currentContext.IsAnimal) return;

            _tutorialSeedAnchor = itemView.transform;
            TutorialAnchorRegistry.Register(TutorialAnchorIds.SeedItem, _tutorialSeedAnchor);
        }

        private void ClearTutorialAnchor()
        {
            if (_tutorialSeedAnchor == null) return;

            TutorialAnchorRegistry.Unregister(TutorialAnchorIds.SeedItem, _tutorialSeedAnchor);
            _tutorialSeedAnchor = null;
        }
        #endregion
    }
}
