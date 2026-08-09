using System.Collections;
using BrunoMikoski.UIManager;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Storage.Animation
{
    public abstract class InventoryWindowTransitionBase :
        WindowTransitionAnimationControllerBase
    {
        private const float OpenDuration = 0.23f;
        private const float CloseDuration = 0.16f;
        private const float StartScale = 0.96f;
        private const float OvershootScale = 1.015f;
        private const float OpenOffsetY = -16f;
        private const float CloseOffsetY = -8f;

        private RectTransform _motionRoot;
        private CanvasGroup _motionCanvasGroup;
        private Graphic _dimmer;
        private Sequence _sequence;
        private Vector2 _restingPosition;
        private float _dimmerAlpha;
        private bool _stateCached;

        protected abstract bool IsOpening { get; }

        public override void BeforeTransitionStart(
            WindowController windowController)
        {
            ResolveTargets();
            KillSequence();

            if (_motionRoot == null || _motionCanvasGroup == null)
                return;

            if (IsOpening)
            {
                _motionRoot.anchoredPosition =
                    _restingPosition + Vector2.up * OpenOffsetY;
                _motionRoot.localScale = Vector3.one * StartScale;
                _motionCanvasGroup.alpha = 0f;
                SetDimmerAlpha(0f);
            }
            else
            {
                RestoreOpenState();
            }
        }

        public override IEnumerator TransitionEnumerator()
        {
            ResolveTargets();
            if (_motionRoot == null || _motionCanvasGroup == null)
                yield break;

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (IsOpening)
                BuildOpenSequence(_sequence);
            else
                BuildCloseSequence(_sequence);

            yield return _sequence.WaitForCompletion();
            _sequence = null;
        }

        public override void AfterTransitionFinished(
            WindowController windowController)
        {
            if (IsOpening)
                RestoreOpenState();
        }

        private void BuildOpenSequence(Sequence sequence)
        {
            sequence.Insert(
                0f,
                _motionRoot
                    .DOAnchorPos(_restingPosition, OpenDuration)
                    .SetEase(Ease.OutCubic));
            sequence.Insert(
                0f,
                _motionCanvasGroup
                    .DOFade(1f, 0.16f)
                    .SetEase(Ease.OutQuad));
            sequence.Insert(
                0f,
                _motionRoot
                    .DOScale(OvershootScale, 0.17f)
                    .SetEase(Ease.OutCubic));
            sequence.Insert(
                0.17f,
                _motionRoot
                    .DOScale(1f, 0.06f)
                    .SetEase(Ease.InOutSine));

            if (_dimmer != null)
            {
                sequence.Insert(
                    0f,
                    _dimmer
                        .DOFade(_dimmerAlpha, 0.16f)
                        .SetEase(Ease.OutQuad));
            }
        }

        private void BuildCloseSequence(Sequence sequence)
        {
            sequence.Join(
                _motionRoot
                    .DOAnchorPos(
                        _restingPosition + Vector2.up * CloseOffsetY,
                        CloseDuration)
                    .SetEase(Ease.InCubic));
            sequence.Join(
                _motionRoot
                    .DOScale(0.97f, CloseDuration)
                    .SetEase(Ease.InCubic));
            sequence.Join(
                _motionCanvasGroup
                    .DOFade(0f, CloseDuration)
                    .SetEase(Ease.InQuad));

            if (_dimmer != null)
            {
                sequence.Join(
                    _dimmer
                        .DOFade(0f, 0.14f)
                        .SetEase(Ease.InQuad));
            }
        }

        private void ResolveTargets()
        {
            if (_motionRoot == null)
            {
                _motionRoot = transform.Find("Motion Root")
                    as RectTransform;
            }

            if (_motionRoot != null && _motionCanvasGroup == null)
            {
                _motionCanvasGroup =
                    _motionRoot.GetComponent<CanvasGroup>();
                if (_motionCanvasGroup == null)
                {
                    _motionCanvasGroup =
                        _motionRoot.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_dimmer == null)
                _dimmer = transform.Find("Dimmer")?.GetComponent<Graphic>();

            if (_motionRoot == null || _stateCached)
                return;

            _restingPosition = _motionRoot.anchoredPosition;
            _dimmerAlpha = _dimmer != null ? _dimmer.color.a : 0f;
            _stateCached = true;
        }

        private void RestoreOpenState()
        {
            if (_motionRoot != null)
            {
                _motionRoot.anchoredPosition = _restingPosition;
                _motionRoot.localScale = Vector3.one;
            }

            if (_motionCanvasGroup != null)
                _motionCanvasGroup.alpha = 1f;

            SetDimmerAlpha(_dimmerAlpha);
        }

        private void SetDimmerAlpha(float alpha)
        {
            if (_dimmer == null) return;
            Color color = _dimmer.color;
            color.a = alpha;
            _dimmer.color = color;
        }

        private void KillSequence()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }

        protected virtual void OnDisable()
        {
            KillSequence();
        }

        protected virtual void OnDestroy()
        {
            KillSequence();
        }
    }
}
