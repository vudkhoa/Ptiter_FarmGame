using System;
using System.Threading;
using Core.Module.Loading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MyOwn.ServiceHarness
{
    // Subscribes to boot progress and drives the loading bar. Registered in the Preloading scene.
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private float _fillSpeed = 1.5f; // normalized fill (0..1) per second

        private IDisposable _sub;
        private float _target;

        // The bar has visually reached the last reported progress.
        public bool IsCaughtUp => _slider == null || _slider.value >= _target - 0.001f;

        [Inject]
        public void Construct(ISubscriber<LoadingProgressPayload> sub)
            => _sub = sub.Subscribe(OnProgress);

        private void Awake()
        {
            // Start empty regardless of the value the scene was saved with.
            if (_slider != null) _slider.value = 0f;
        }

        private void OnProgress(LoadingProgressPayload p)
        {
            _target = Mathf.Clamp01(p.Progress / 100f);   // Progress is 0..100
            if (_label != null) _label.text = p.Message;
        }

        private void Update()
        {
            if (_slider == null || _slider.value >= _target) return;
            // Unscaled: boot must keep animating even if something parks Time.timeScale at 0.
            _slider.value = Mathf.MoveTowards(_slider.value, _target, _fillSpeed * Time.unscaledDeltaTime);
        }

        // Lets the boot flow hold the scene swap until the bar actually finished travelling.
        public UniTask WaitUntilCaughtUpAsync(CancellationToken ct)
            => UniTask.WaitUntil(() => IsCaughtUp, cancellationToken: ct);

        private void OnDestroy() => _sub?.Dispose();
    }
}
