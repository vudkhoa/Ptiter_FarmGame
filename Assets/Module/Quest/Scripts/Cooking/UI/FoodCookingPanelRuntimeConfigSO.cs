using UnityEngine;

namespace Core.Module.Quest.Cooking
{
    [CreateAssetMenu(
        fileName = "FoodCookingPanelRuntimeConfig",
        menuName = "GDD/Quest/Cooking Panel Runtime Config")]
    public sealed class FoodCookingPanelRuntimeConfigSO : ScriptableObject
    {
        [SerializeField] private GameObject _panelPrefab;

        public GameObject PanelPrefab => _panelPrefab;
    }
}
