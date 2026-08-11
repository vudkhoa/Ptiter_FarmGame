#if UNITY_EDITOR
using System.Collections;
using Core.Module.Quest.Cooking.UI;
using UnityEngine;

namespace Core.Module.Quest.Cooking.QA
{
    [AddComponentMenu("")]
    public sealed class FoodCookingPanelAnimationQaHarness :
        MonoBehaviour
    {
        [SerializeField] private FoodCookingPanelView _view;
        [SerializeField, Min(1)] private int _mockQuantity = 3;
        [SerializeField, Range(2, 10)]
        private int _mockCountdownSeconds = 5;
        [SerializeField] private bool _loop = true;
        [SerializeField, Min(0f)]
        private float _detailHoldSeconds = 0.85f;
        [SerializeField, Min(0f)]
        private float _cycleGapSeconds = 1.25f;

        private Coroutine _routine;

        public void Configure(
            FoodCookingPanelView view,
            int quantity = 3,
            int countdownSeconds = 5,
            bool loop = true)
        {
            _view = view;
            _mockQuantity = Mathf.Max(1, quantity);
            _mockCountdownSeconds =
                Mathf.Clamp(countdownSeconds, 2, 10);
            _loop = loop;
            Run();
        }

        [ContextMenu("Run Cooking Animation QA")]
        public void Run()
        {
            StopRoutine();

            if (!isActiveAndEnabled || _view == null)
                return;

            _routine = StartCoroutine(RunShowcase());
        }

        private IEnumerator RunShowcase()
        {
            do
            {
                _view.ShowDetailImmediate();
                yield return new WaitForSecondsRealtime(
                    _detailHoldSeconds);

                _view.PlayIntro(
                    _mockQuantity,
                    _mockCountdownSeconds);
                yield return new WaitForSecondsRealtime(
                    _view.IntroDuration);

                for (int remaining = _mockCountdownSeconds;
                     remaining > 0;
                     remaining--)
                {
                    _view.UpdateCountdown(
                        _mockQuantity,
                        remaining);
                    yield return new WaitForSecondsRealtime(1f);
                }

                _view.PlayCompletion(_mockQuantity);
                yield return new WaitForSecondsRealtime(
                    _view.CompletionDuration + _cycleGapSeconds);
            }
            while (_loop && isActiveAndEnabled);

            _routine = null;
        }

        private void OnDisable()
        {
            StopRoutine();
        }

        private void StopRoutine()
        {
            if (_routine == null)
                return;

            StopCoroutine(_routine);
            _routine = null;
        }
    }
}
#endif
