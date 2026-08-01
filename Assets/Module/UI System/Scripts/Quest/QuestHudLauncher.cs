using BrunoMikoski.UIManager;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MyOwn.ServiceHarness
{
    [DisallowMultipleComponent]
    public sealed class QuestHudLauncher : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private WindowsManager _windowsManager;
        [SerializeField] private UIWindow _questWindow;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Configure(
            Button openButton,
            WindowsManager windowsManager,
            UIWindow questWindow,
            IObjectResolver resolver)
        {
            _openButton?.onClick.RemoveListener(Open);
            _openButton = openButton;
            _windowsManager = windowsManager;
            _questWindow = questWindow;
            _resolver = resolver;
            if (isActiveAndEnabled)
                _openButton?.onClick.AddListener(Open);
        }

        private void OnEnable()
        {
            _openButton?.onClick.AddListener(Open);
        }

        private void OnDisable()
        {
            _openButton?.onClick.RemoveListener(Open);
        }

        public void Open()
        {
            if (_windowsManager == null || _questWindow == null) return;
            _windowsManager.Open(_questWindow);
            if (_windowsManager.TryGetWindowInstance(
                    _questWindow, out QuestWindowController window))
                _resolver?.Inject(window);
        }
    }
}
