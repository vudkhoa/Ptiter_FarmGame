using System;
using System.Collections.Generic;
using BrunoMikoski.UIManager;
using Core.Module.Input;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    /// View cutscene sống dưới WindowsManager, prefab chỉ Instantiate ở lần Play đầu tiên.
    [DisallowMultipleComponent]
    public sealed class CutsceneWindowController :
        WindowController,
        ICutsceneView,
        IOnBeforeWindowOpen,
        IOnWindowClosed
    {
        [SerializeField] private SlotConfig[] _slots;
        [SerializeField] private NamedButton[] _buttons;

        private readonly Dictionary<Image, int> _imageToSlotIndex = new();
        private readonly Dictionary<string, Button> _buttonById = new();

        // VFX không pool như Image: prefab particle tái dùng phải lo restart emitter, mà một
        // cutscene chỉ spawn nó đúng một lần nên pool chỉ tổ thêm chỗ sai. Huỷ luôn cho gọn.
        private readonly List<RectTransform> _activeVfx = new(2);

        private SlotRuntime[] _runtime;

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

        protected override void OnDestroy()
        {
            GameplayInputBlockRegistry.Remove(this);
            base.OnDestroy();
        }
        #endregion

        #region Public API
        public void OnBeforeWindowOpen()
        {
            GameplayInputBlockRegistry.Add(this);
        }

        public void OnWindowClosed()
        {
            GameplayInputBlockRegistry.Remove(this);
        }

        public Image AcquireImage(CutsceneImageSlot slot)
        {
            int index = (int)slot;
            if (_slots == null || index < 0 || index >= _slots.Length) return null;

            SlotConfig config = _slots[index];
            if (config == null || config.parent == null || config.template == null)
            {
                Debug.LogError($"[CutsceneWindow] Slot {slot} thiếu parent hoặc template.", this);
                return null;
            }

            SlotRuntime runtime = _runtime[index];
            Image image = runtime.Pool.Count > 0
                ? runtime.Pool.Pop()
                : Instantiate(config.template, config.parent);

            ResetImageState(image, config.template);
            image.gameObject.SetActive(true);
            image.rectTransform.SetAsLastSibling();

            runtime.Active.Add(image);
            _imageToSlotIndex[image] = index;
            return image;
        }

        public void ReleaseImage(Image image)
        {
            if (image == null) return;
            if (!_imageToSlotIndex.TryGetValue(image, out int index)) return;

            image.DOKill();
            image.gameObject.SetActive(false);

            SlotRuntime runtime = _runtime[index];
            runtime.Active.Remove(image);
            runtime.Pool.Push(image);
            _imageToSlotIndex.Remove(image);
        }

        public RectTransform AcquireVfx(CutsceneImageSlot slot, GameObject prefab)
        {
            int index = (int)slot;
            if (prefab == null || _slots == null || index < 0 || index >= _slots.Length) return null;

            SlotConfig config = _slots[index];
            if (config == null || config.parent == null)
            {
                Debug.LogError($"[CutsceneWindow] Slot {slot} thiếu parent.", this);
                return null;
            }

            // worldPositionStays = false: để true thì Unity bẻ lại localScale/localPosition cho khớp
            // world cũ, prefab UI vào Canvas là lệch chỗ và sai cỡ ngay.
            GameObject spawned = Instantiate(prefab, config.parent, false);
            RectTransform instance = spawned.transform as RectTransform;
            if (instance == null)
            {
                // Huỷ luôn bản vừa tạo, bỏ đó là để lại rác treo dưới slot.
                Debug.LogError($"[CutsceneWindow] Prefab VFX '{prefab.name}' thiếu RectTransform.", this);
                Destroy(spawned);
                return null;
            }

            // Cùng luật với AcquireImage: spawn sau thì nằm trên.
            instance.SetAsLastSibling();
            _activeVfx.Add(instance);
            return instance;
        }

        public void ReleaseVfx(RectTransform instance)
        {
            if (instance == null) return;
            if (!_activeVfx.Remove(instance)) return;

            Destroy(instance.gameObject);
        }

        public void ResetSlots()
        {
            for (int i = _activeVfx.Count - 1; i >= 0; i--)
            {
                RectTransform vfx = _activeVfx[i];
                if (vfx != null) Destroy(vfx.gameObject);
            }

            _activeVfx.Clear();

            if (_runtime == null) return;

            for (int i = 0; i < _runtime.Length; i++)
            {
                List<Image> active = _runtime[i].Active;
                for (int j = active.Count - 1; j >= 0; j--)
                {
                    Image image = active[j];
                    if (image == null) continue;

                    image.DOKill();
                    image.gameObject.SetActive(false);
                    _runtime[i].Pool.Push(image);
                    _imageToSlotIndex.Remove(image);
                }

                active.Clear();
            }
        }

        public Button GetButton(string buttonId)
        {
            if (string.IsNullOrEmpty(buttonId)) return null;

            return _buttonById.TryGetValue(buttonId, out Button button) ? button : null;
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
                SlotConfig config = _slots[i];
                // Template sống trong prefab, disable để nó không hiện khi cutscene chưa chạy.
                if (config != null && config.template != null) config.template.gameObject.SetActive(false);
            }
        }

        private void InitButtons()
        {
            if (_buttons == null) return;

            for (int i = 0; i < _buttons.Length; i++)
            {
                NamedButton entry = _buttons[i];
                if (entry == null || entry.button == null || string.IsNullOrWhiteSpace(entry.id)) continue;

                _buttonById[entry.id] = entry.button;
                // Button chỉ hiện đúng lúc task chờ nó, còn lại luôn ẩn.
                entry.button.gameObject.SetActive(false);
            }
        }

        // Image lấy từ pool còn giữ transform của panel cũ (kể cả anchor/size/góc nghiêng task ghi đè).
        private static void ResetImageState(Image image, Image template)
        {
            image.sprite = null;
            Color color = image.color;
            color.a = 0f;
            image.color = color;

            RectTransform rect = image.rectTransform;
            RectTransform source = template.rectTransform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition;
            rect.localRotation = Quaternion.identity;
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
            public readonly List<Image> Active = new(4);
            public readonly Stack<Image> Pool = new(4);
        }
    }
}
