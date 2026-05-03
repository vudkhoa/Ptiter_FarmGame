using UnityEngine;

namespace Core.Module.Time
{
    [CreateAssetMenu(fileName = "TimeServiceConfig", menuName = "Data/Time/Config")]
    public sealed class TimeServiceConfig : ScriptableObject
    {
        [Header("Settings")]
        [Range(10f, 60f)]
        public float _driftThresholdSeconds = 30f;

        [Range(1f, 30f)]
        public float _driftCheckIntervalSeconds = 2f;

        [Range(30f, 3600f)]
        public float _resyncIntervalSeconds = 5 * 60f;
        public bool _resetBaselineOnAppFocus = true;
    }
}