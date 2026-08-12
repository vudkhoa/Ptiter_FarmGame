using System;
using Core.Module.Cutscene;
using Core.Module.Quest.Cooking.UI;
using MessagePipe;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Module.Quest.Cooking
{
    public sealed class FoodCookingPanelFactory :
        IFoodCookingPanelFactory,
        IDisposable
    {
        private readonly FoodCookingPanelRuntimeConfigSO _config;
        private readonly FoodRecipeConfigSO _foodRecipeConfig;
        private readonly ICookingService _cookingService;
        private readonly IPublisher<PlayCutsceneRequestPayload>
            _cutscenePublisher;
        private readonly ISubscriber<CookingStateChangedPayload>
            _stateSubscriber;
        private readonly ISubscriber<CookingCompletedPayload>
            _completedSubscriber;
        private GameObject _instance;
        private FoodCookingPanelPresenter _presenter;
        private string _recipeId;

        public bool IsOpen => _instance != null;

        public FoodCookingPanelFactory(
            QuestCatalogSO catalog,
            ICookingService cookingService,
            IPublisher<PlayCutsceneRequestPayload> cutscenePublisher,
            ISubscriber<CookingStateChangedPayload> stateSubscriber,
            ISubscriber<CookingCompletedPayload> completedSubscriber)
        {
            _config = catalog != null
                ? catalog.foodCookingPanelConfig
                : null;
            _foodRecipeConfig = catalog != null
                ? catalog.foodRecipeConfig
                : null;
            _cookingService = cookingService;
            _cutscenePublisher = cutscenePublisher;
            _stateSubscriber = stateSubscriber;
            _completedSubscriber = completedSubscriber;
        }

        public GameObject Open(RectTransform parent, string recipeId)
        {
            if (parent == null || string.IsNullOrWhiteSpace(recipeId))
            {
                Debug.LogError(
                    "[Cooking UI] Cannot open without an overlay parent.");
                return null;
            }

            FoodRecipeDefinition recipe =
                _foodRecipeConfig?.GetRecipe(recipeId);
            string cutsceneId = recipe != null &&
                                recipe.HasPlayableCutscene
                ? recipe.cutscene.cutsceneId
                : string.Empty;
            if (string.IsNullOrWhiteSpace(cutsceneId))
            {
                Debug.LogError(
                    $"[Cooking UI] Recipe '{recipeId}' has no cutscene.");
                return null;
            }

            if (_instance != null)
            {
                if (!string.Equals(
                        _recipeId,
                        recipeId,
                        StringComparison.Ordinal))
                {
                    Close();
                    return Open(parent, recipeId);
                }

                _instance.SetActive(true);
                _instance.transform.SetAsLastSibling();
                return _instance;
            }

            GameObject panelPrefab = _config != null
                ? _config.PanelPrefab
                : null;
            if (panelPrefab == null)
            {
                Debug.LogError(
                    "[Cooking UI] FoodCookingPanelRuntimeConfig has no panel prefab.");
                return null;
            }

            _instance = Object.Instantiate(panelPrefab, parent, false);
            _instance.name = panelPrefab.name;

            if (_instance.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            _instance.transform.SetAsLastSibling();
            FoodCookingPanelView view =
                _instance.GetComponent<FoodCookingPanelView>();
            if (view == null)
            {
                Debug.LogError(
                    "[Cooking UI] Panel prefab is missing " +
                    $"{nameof(FoodCookingPanelView)}.",
                    _instance);
                Object.Destroy(_instance);
                _instance = null;
                return null;
            }

            _recipeId = recipeId;
            _presenter = new FoodCookingPanelPresenter(
                view,
                _cookingService,
                _stateSubscriber,
                _completedSubscriber,
                recipeId,
                cutsceneId,
                _cutscenePublisher,
                Close);
            return _instance;
        }

        public void Close()
        {
            if (_instance == null) return;

            _presenter?.Dispose();
            _presenter = null;
            _recipeId = null;
            Object.Destroy(_instance);
            _instance = null;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
