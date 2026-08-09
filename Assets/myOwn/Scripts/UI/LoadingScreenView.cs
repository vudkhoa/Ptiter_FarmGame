using System;
using Core.Module.Loading;
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

        [Inject]
        public void Construct(ISubscriber<LoadingProgressPayload> sub)
            => _sub = sub.Subscribe(OnProgress);

        private void OnProgress(LoadingProgressPayload p)
        {
            _target = p.Progress / 100f;   // Progress is 0..100
            if (_label != null) _label.text = p.Message;
        }

        private void Update()
        {
            if (_slider == null || _slider.value >= _target) return;
            _slider.value = Mathf.MoveTowards(_slider.value, _target, _fillSpeed * Time.deltaTime);
        }

        private void OnDestroy() => _sub?.Dispose();
    }
}
