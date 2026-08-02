using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Cutscene
{
    [DisallowMultipleComponent]
    public sealed class CutsceneView : MonoBehaviour, ICutsceneView
    {
        [SerializeField] private CanvasGroup _root;

        [Tooltip("Xếp theo đúng thứ tự CutsceneImageSlot: Background, Foreground, Overlay. " +
                 "Mỗi slot cần 1 parent RectTransform + 1 Image template (inactive, dùng làm gốc để Instantiate).")]
        [SerializeField] private SlotConfig[] _slots;

        [Min(0f)]
        [SerializeField] private float _showHideDuration = 0.3f;

        private SlotRuntime[] _runtime;
        private readonly Dictionary<Image, int> _imageToSlotIndex = new Dictionary<Image, int>();

        public CanvasGroup RootCanvasGroup => _root;

        private void Awake()
        {
            if (_slots == null) return;

            _runtime = new SlotRuntime[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _runtime[i] = new SlotRuntime();
                var cfg = _slots[i];
                // Template sống dưới scene, disable để nó không hiển thị khi cutscene chưa chạy.
                if (cfg != null && cfg.template != null) cfg.template.gameObject.SetActive(false);
            }
        }

        public Image AcquireImage(CutsceneImageSlot slot)
        {
            int idx = (int)slot;
            if (_slots == null || idx < 0 || idx >= _slots.Length) return null;

            var cfg = _slots[idx];
            if (cfg == null || cfg.parent == null || cfg.template == null)
            {
                Debug.LogError($"[CutsceneView] Slot {slot} thiếu parent hoặc template - không Acquire được Image.");
                return null;
            }

            var rt = _runtime[idx];
            Image image = rt.Pool.Count > 0 ? rt.Pool.Pop() : Instantiate(cfg.template, cfg.parent);

            ResetImageState(image);
            image.gameObject.SetActive(true);
            // SetAsLastSibling để Image mới acquire nằm trên cùng trong slot.
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

        public UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            if (_root == null) return UniTask.CompletedTask;

            _root.blocksRaycasts = true;
            _root.alpha = 0f;
            return FadeRootAsync(1f, ct);
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            if (_root != null)
            {
                _root.blocksRaycasts = false;
                await FadeRootAsync(0f, ct);
            }

            gameObject.SetActive(false);
        }

        private UniTask FadeRootAsync(float target, CancellationToken ct)
            => _root.DOFade(target, _showHideDuration)
                    .SetLink(gameObject)
                    .ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, ct);

        private static void ResetImageState(Image image)
        {
            image.sprite = null;
            image.enabled = true;
            var color = image.color;
            color.a = 0f;
            image.color = color;
            var rect = image.rectTransform;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        [Serializable]
        private sealed class SlotConfig
        {
            public RectTransform parent;
            public Image template;
        }

        private sealed class SlotRuntime
        {
            public readonly List<Image> Active = new List<Image>(4);
            public readonly Stack<Image> Pool = new Stack<Image>(4);
        }
    }
}
