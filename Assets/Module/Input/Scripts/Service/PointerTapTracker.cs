using UnityEngine;

namespace Core.Module.Input
{
    /// <summary>
    /// Prevents a long-press release from also being handled as a regular world tap.
    /// The suppression stays active until the next primary-pointer press begins so
    /// every release listener observes the same state regardless of callback order.
    /// </summary>
    public static class PointerReleaseSuppression
    {
        public static bool IsPrimaryReleaseSuppressed { get; private set; }

        public static void SuppressPrimaryRelease()
        {
            IsPrimaryReleaseSuppressed = true;
        }

        public static void BeginPrimaryPress()
        {
            IsPrimaryReleaseSuppressed = false;
        }
    }

    /// <summary>
    /// Classifies a primary-pointer gesture as a tap when it stays within
    /// the configured screen-space movement tolerance.
    /// </summary>
    public sealed class PointerTapTracker
    {
        private readonly float _maxMovementSqr;
        private Vector2 _startPosition;
        private bool _isTracking;
        private bool _movedTooFar;

        public PointerTapTracker(float maxMovementPixels)
        {
            float tolerance = Mathf.Max(1f, maxMovementPixels);
            _maxMovementSqr = tolerance * tolerance;
        }

        public void Begin(Vector2 screenPosition)
        {
            _startPosition = screenPosition;
            _movedTooFar = false;
            _isTracking = true;
        }

        public void Move(Vector2 screenPosition)
        {
            if (!_isTracking || _movedTooFar) return;

            if ((screenPosition - _startPosition).sqrMagnitude > _maxMovementSqr)
                _movedTooFar = true;
        }

        public bool Complete(Vector2 screenPosition)
        {
            Move(screenPosition);
            bool isTap = _isTracking && !_movedTooFar;
            Cancel();
            return isTap;
        }

        public void Cancel()
        {
            _isTracking = false;
            _movedTooFar = false;
        }
    }
}
