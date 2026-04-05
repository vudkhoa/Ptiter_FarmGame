using UnityEngine;

namespace Main.Map
{
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _mouseIndicator;
        [SerializeField] private InputManager _inputManager;

        private void Update()
        {
            Vector3 mousePostion = _inputManager.GetSelectedMapPosition();
            _mouseIndicator.transform.position = mousePostion;
        }
    }
}