using UnityEngine;

namespace Core.Module.Input
{
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
