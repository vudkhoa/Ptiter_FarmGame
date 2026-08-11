using Core.Module.Input;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Shared.Utils.HUD
{
    /// <summary>
    /// Shared HUD button presentation and click policy. The concrete operation
    /// is supplied by one sibling component implementing IHudButtonAction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class HudActionButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _visual;
        [SerializeField] private MonoBehaviour _actionComponent;
        [SerializeField] private bool _blockWhileModalOpen = true;

        private IInputService _inputService;
        private IHudButtonAction _action;

        public Button Button => _button;
        public Image Visual => _visual;
        public MonoBehaviour ActionComponent => _actionComponent;

        [Inject]
        public void Construct(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Configure(Image visual, MonoBehaviour actionComponent)
        {
            _visual = visual;
            SetAction(actionComponent);
        }

        public void SetAction(MonoBehaviour actionComponent)
        {
            _actionComponent = actionComponent;
            _action = actionComponent as IHudButtonAction;
        }

        public void SetSprite(Sprite sprite)
        {
            ResolveReferences();
            if (_visual != null)
                _visual.sprite = sprite;
        }

        public void SetInteractable(bool interactable)
        {
            ResolveReferences();
            if (_button != null)
                _button.interactable = interactable;
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (_button == null) return;

            _button.onClick.RemoveListener(Execute);
            _button.onClick.AddListener(Execute);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Execute);
        }

        public void Execute()
        {
            ResolveReferences();
            if (_blockWhileModalOpen && IsModalInputBlocked()) return;

            if (_action == null)
            {
                Debug.LogError(
                    $"[HudActionButton] '{name}' has no IHudButtonAction assigned.",
                    this);
                return;
            }

            if (_action.CanExecute())
                _action.TryExecute();
        }

        private bool IsModalInputBlocked()
        {
            if (GameplayInputBlockRegistry.IsBlocked) return true;
            if (Shared.Utils.WindowOpenTrigger.IsAnyWindowOpen) return true;
            return _inputService != null &&
                   _inputService.IsGameplayInputBlocked;
        }

        private void ResolveReferences()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_visual == null && _button != null)
                _visual = _button.targetGraphic as Image;

            _action = _actionComponent as IHudButtonAction;
            if (_action != null) return;

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is not IHudButtonAction candidate) continue;

                _actionComponent = behaviour;
                _action = candidate;
                break;
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
