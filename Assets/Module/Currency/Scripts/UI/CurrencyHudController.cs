using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Core.Module.Currency
{
    public sealed class CurrencyHudController : IStartable, IDisposable
    {
        private const string GameplaySceneName = "MapScene";
        private const string HudPrefabResourcePath = "Currency/CurrencyHud";

        private readonly ICurrencyService _currencyService;
        private readonly IDisposable _currencyChangedSubscription;
        private CurrencyHudView _view;

        public CurrencyHudController(
            ICurrencyService currencyService,
            ISubscriber<CurrencyChangedPayload> currencyChangedSubscriber)
        {
            _currencyService = currencyService;
            _currencyChangedSubscription =
                currencyChangedSubscriber.Subscribe(OnCurrencyChanged);
        }

        public void Start()
        {
            LoadViewPrefab();
            SceneManager.sceneLoaded += OnSceneLoaded;

            string activeSceneName = SceneManager.GetActiveScene().name;
            SetVisible(activeSceneName == GameplaySceneName);
            Render(_currencyService.Balance);
            Debug.Log(
                $"[CurrencyHUD] Started from prefab. Scene={activeSceneName}, " +
                $"Balance={_currencyService.Balance}, ViewLoaded={_view != null}.");
        }

        private void LoadViewPrefab()
        {
            if (_view != null) return;

            GameObject prefab =
                Resources.Load<GameObject>(HudPrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[CurrencyHUD] Prefab is missing at Resources/{HudPrefabResourcePath}.prefab. " +
                    "Run Tools/Currency/Ensure Currency HUD Prefab.");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "[Currency HUD]";
            UnityEngine.Object.DontDestroyOnLoad(instance);
            _view = instance.GetComponent<CurrencyHudView>();
            if (_view != null) return;

            Debug.LogError(
                "[CurrencyHUD] CurrencyHudView is missing on the prefab root.");
            UnityEngine.Object.Destroy(instance);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetVisible(scene.name == GameplaySceneName);
            if (scene.name == GameplaySceneName)
                Render(_currencyService.Balance);
        }

        private void OnCurrencyChanged(CurrencyChangedPayload payload)
        {
            Render(payload.CurrentBalance);
        }

        private void Render(int balance)
        {
            _view?.SetBalance(balance);
        }

        private void SetVisible(bool visible)
        {
            if (_view != null)
                _view.gameObject.SetActive(visible);
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _currencyChangedSubscription?.Dispose();
            if (_view != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }
        }
    }
}
