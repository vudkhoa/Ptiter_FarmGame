using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Module.Storage;
using Core.Module.Farm;
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
        [SerializeField] private GameObject _itemTemplate;
        [SerializeField] private Button _closeButton;

        private IFarmService _farmService;
        private IStorageService _storageService;
        private FarmDatabaseSO _database;
        private OpenFarmSelectorUIPayload _currentContext;

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
            if (_itemTemplate != null) _itemTemplate.SetActive(false);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        public void OnWindowClosed()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);
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
                if (child.gameObject != _itemTemplate)
                {
                    Destroy(child.gameObject);
                }
            }

            // Populate based on Crops vs Animals
            if (_currentContext.IsAnimal)
            {
                if (_database.AllAnimals != null)
                {
                    foreach (var animal in _database.AllAnimals)
                    {
                        CreateItemButton(animal.animalId, animal.animalName, animal.purchaseCost, _currentContext);
                    }
                }
            }
            else
            {
                if (_database.AllCrops != null)
                {
                    foreach (var crop in _database.AllCrops)
                    {
                        Debug.Log("HAS CROP");
                        CreateItemButton(crop.cropId, crop.cropName, crop.coinCost, _currentContext);
                    }
                }
            }
        }

        private void CreateItemButton(string entityId, string displayName, int cost, OpenFarmSelectorUIPayload payload)
        {
            GameObject buttonObj = Instantiate(_itemTemplate, _itemContainer);
            buttonObj.SetActive(true);

            TMP_Text[] texts = buttonObj.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Name")) t.text = displayName;
                else if (t.name.Contains("Cost")) t.text = $"{cost} Xu";
            }

            Button btn = buttonObj.GetComponent<Button>();
            if (btn == null) btn = buttonObj.GetComponentInChildren<Button>();

            if (btn != null)
            {
                bool canAfford = _storageService.Coins >= cost;
                btn.interactable = canAfford;

                var btnImage = btn.GetComponent<Image>();
                if (btnImage != null && !canAfford)
                {
                    btnImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }

                btn.onClick.AddListener(() =>
                {
                    if (_farmService.TryPlant(payload.Cell, entityId, payload.IsAnimal))
                    {
                        Close();
                    }
                });
            }
        }
    }
}
