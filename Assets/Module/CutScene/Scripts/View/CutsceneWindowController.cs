using System;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// <summary>
    /// View cutscene sống dưới WindowsManager, prefab chỉ Instantiate ở lần Play đầu tiên.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneWindowController : WindowController, ICutsceneView
    {
        [SerializeField] private SlotConfig[] _slots;

        [Tooltip("Id phải khớp field Button Id trong WaitForButtonClickTask.")]
        [SerializeField] private NamedButton[] _buttons;

        private SlotRuntime[] _runtime;
        private readonly Dictionary<Image, int> _imageToSlotIndex = new Dictionary<Image, int>();
        private readonly Dictionary<string, Button> _buttonById = new Dictionary<string, Button>();

        #region Properties
        // WindowController đã RequireComponent(CanvasGroup) nên không giữ field riêng.
        public CanvasGroup RootCanvasGroup => CanvasGroup;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitSlots();
            InitButtons();
        }
        #endregion

        #region Public API
        public Image AcquireImage(CutsceneImageSlot slot)
        {
            int idx = (int)slot;
            if (_slots == null || idx < 0 || idx >= _slots.Length) return null;

            var cfg = _slots[idx];
            if (cfg == null || cfg.parent == null || cfg.template == null)
            {
                Debug.LogError($"[CutsceneWindow] Slot {slot} thiếu parent hoặc template.", this);
                return null;
            }

            var rt = _runtime[idx];
            Image image = rt.Pool.Count > 0 ? rt.Pool.Pop() : Instantiate(cfg.template, cfg.parent);

            ResetImageState(image);
            image.gameObject.SetActive(true);
            // Đẩy xuống cuối
            image.rectTransform.SetAsLastSibling();

            rt.Active.Add(image);
            _imageToSlotIndex[image] = idx;
            return image;
        }

        public void ReleaseImage(Image image)
        {
            if (image == null) return;
            if (!_imageToSlotIndex.TryGetValue(image, out var idx)) return;

            image.DOKill();
            image.gameObject.SetActive(false);

            var rt = _runtime[idx];
            rt.Active.Remove(image);
            rt.Pool.Push(image);
            _imageToSlotIndex.Remove(image);
        }

        public void ResetSlots()
        {
            if (_runtime == null) return;

            for (int i = 0; i < _runtime.Length; i++)
            {
                var active = _runtime[i].Active;
                for (int j = active.Count - 1; j >= 0; j--)
                {
                    var img = active[j];
                    if (img == null) continue;

                    img.DOKill();
                    img.gameObject.SetActive(false);
                    _runtime[i].Pool.Push(img);
                    _imageToSlotIndex.Remove(img);
                }
                active.Clear();
            }
        }

        public Button GetButton(string buttonId)
        {
            if (string.IsNullOrEmpty(buttonId)) return null;
            return _buttonById.TryGetValue(buttonId, out var button) ? button : null;
        }

        #endregion

        #region Private Methods
        private void InitSlots()
        {
            if (_slots == null) return;

            _runtime = new SlotRuntime[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _runtime[i] = new SlotRuntime();
                var cfg = _slots[i];
                // Template sống trong prefab, disable để nó không hiện khi cutscene chưa chạy.
                if (cfg != null && cfg.template != null) cfg.template.gameObject.SetActive(false);
            }
        }

        private void InitButtons()
        {
            if (_buttons == null) return;

            for (int i = 0; i < _buttons.Length; i++)
            {
                var entry = _buttons[i];
                if (entry == null || entry.button == null || string.IsNullOrWhiteSpace(entry.id)) continue;

                _buttonById[entry.id] = entry.button;
                // Button chỉ hiện đúng lúc task chờ nó, còn lại luôn ẩn.
                entry.button.gameObject.SetActive(false);
            }
        }

        // Image lấy từ pool còn giữ transform của panel cũ, phải trả về gốc trước khi dùng lại.
        private static void ResetImageState(Image image)
        {
            image.sprite = null;
            var color = image.color;
            color.a = 0f;
            image.color = color;

            var rect = image.rectTransform;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        #endregion

        [Serializable]
        private sealed class SlotConfig
        {
            public RectTransform parent;
            public Image template;
        }

        [Serializable]
        private sealed class NamedButton
        {
            public string id;
            public Button button;
        }

        private sealed class SlotRuntime
        {
            public readonly List<Image> Active = new List<Image>(4);
            public readonly Stack<Image> Pool = new Stack<Image>(4);
        }
    }
}
