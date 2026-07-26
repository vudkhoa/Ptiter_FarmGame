using UnityEngine;

namespace Core.Module.Loading
{
    [CreateAssetMenu(fileName = "LoadingConfig", menuName = "Data/Loading/Config")]
    public class LoadingConfigSO : ScriptableObject
    {
        public string[] CriticalLabels;
        public string RemotePathOverride;
        public float TimeoutSeconds = 30;
        public int TryCount = 2;
    }
}