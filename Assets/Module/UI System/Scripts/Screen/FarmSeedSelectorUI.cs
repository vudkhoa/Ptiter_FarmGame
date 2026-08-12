// TODO: Recheck UI
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Core.Module.Storage;
using Core.Module.Farm;
using Core.Module.Tutorial;
using VContainer;
using BrunoMikoski.UIManager;

namespace MyOwn.ServiceHarness
{
    /// <summary>
    /// UI chọn hạt giống/con non, kế thừa WindowController và biên dịch trong Assembly-CSharp.
    /// Tự động được VContainer Inject qua thuộc tính autoInjectGameObjects của Test_UIManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmSeedSelectorUI : WindowController, IOnBeforeWindowOpen, IOnWindowClosed
    {
        [Header("UI Containers")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private FarmSeedSelectorItemView _itemTemplate;
        [SerializeField] private Button _closeButton;

        private IFarmService _farmService;
        private IStorageService _storageService;
        private FarmDatabaseSO _database;
        private OpenFarmSelectorUIPayload _currentContext;
        private Transform _tutorialSeedAnchor;

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
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        public void OnWindowClosed()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);

            ClearTutorialAnchor();
        }

        /// <summary>
        /// Được gọi bởi Bridge ngay sau khi UIManager mở cửa sổ và lấy được Instance.
        /// </summary>
        public void InitializeSelector(OpenFarmSelectorUIPayload context)
        {
            _currentContext = context;
            PopulateUI();
        }

        private void PopulateUI()
        {
            if (_itemContainer == null || _itemTemplate == null) return;

            // Đảm bảo item container luôn hoạt động (Active) để hiện các nút con
            _itemContainer.gameObject.SetActive(true);

            // Clear old item lists
            foreach (Transform child in _itemContainer)
            {
                if (child != _itemTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            ClearTutorialAnchor();

            // Duyệt danh sách AllEntities duy nhất từ FarmDatabaseSO và lọc theo loại Crop / Animal
            if (_database.AllEntities != null)
            {
                foreach (var entity in _database.AllEntities)
                {
                    if (entity == null) continue;

                    bool matchesAnimalContext = entity.entityType == FarmEntityType.Animal;
                    if (matchesAnimalContext == _currentContext.IsAnimal)
                    {
                        CreateItemButton(entity, _currentContext);
                    }
                }
            }
        }

        private void CreateItemButton(FarmEntityData entity, OpenFarmSelectorUIPayload payload)
        {
            FarmSeedSelectorItemView itemView = Instantiate(_itemTemplate, _itemContainer);
            itemView.gameObject.SetActive(true);

            bool canAfford = _storageService.Coins >= entity.coinCost;
            itemView.Bind(
                entity,
                canAfford,
                selectedEntity =>
            {
                if (_farmService.TryPlant(payload.Cell, selectedEntity.EntityId))
                {
                    Close();
                }
            });

            TryMarkTutorialSeed(itemView, canAfford);
        }

        /// <summary>
        /// Points the "choose a seed" step at the first row the player can actually buy - the
        /// hand must never sit on a greyed-out button.
        /// </summary>
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
    }
}
