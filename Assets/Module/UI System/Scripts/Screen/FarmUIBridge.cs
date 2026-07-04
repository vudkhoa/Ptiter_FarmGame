using System;
using BrunoMikoski.UIManager;
using Core.Module.Farm;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace MyOwn.ServiceHarness
{
    [DisallowMultipleComponent]
    public sealed class FarmUIBridge : MonoBehaviour
    {
        [SerializeField] private WindowsManager _windowsManager;
        [SerializeField] private UIWindow _farmSelectorWindow;

        private IDisposable _subscription;
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(ISubscriber<OpenFarmSelectorUIPayload> openUiSub, IObjectResolver resolver)
        {
            _subscription = openUiSub.Subscribe(OnOpenUIRequested);
            _resolver = resolver;
        }

        private void OnOpenUIRequested(OpenFarmSelectorUIPayload payload)
        {
            if (_windowsManager == null || _farmSelectorWindow == null) return;

            // 1. Gọi UIManager mở Window chọn hạt giống
            _windowsManager.Open(_farmSelectorWindow);

            // 2. Tìm instance của Window đang mở để gán Context gieo trồng (Ruộng/Chuồng, Tọa độ Grid)
            if (_windowsManager.TryGetWindowInstance(_farmSelectorWindow, out FarmSeedSelectorUI screen))
            {
                // Inject các dependency từ DI Container cho UI vừa được Instantiate động
                _resolver.Inject(screen);
                screen.InitializeSelector(payload);
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}