using System;
using Core.Module.Loading;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace MyOwn.ServiceHarness
{
    // Subscribes to boot progress and drives the loading bar. Registered in the Preloading scene.
    public sealed class LoadingScreenView : MonoBehaviour
    {
        // [SerializeField] private Slider _bar;
        [SerializeField] private TextMeshProUGUI _label;
        private IDisposable _sub;

        [Inject]
        public void Construct(ISubscriber<LoadingProgressPayload> sub)
            => _sub = sub.Subscribe(OnProgress);

        private void OnProgress(LoadingProgressPayload p)
        {
            //if (_bar != null) _bar.value = p.Progress / 100f;   // Progress is 0..100
            if (_label != null) _label.text = p.Message;
        }

        private void OnDestroy() => _sub?.Dispose();
    }
}