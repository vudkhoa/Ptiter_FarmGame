using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Main.Map
{
    public class InputManager : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private float _maxDistance = 1000f;
        
        [Header("Settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _placeLayerMask;

        // Runtime
        private Vector3 lastPosition;

        public event Action OnClicked;
        public event Action OnExit;

        public Vector3 GetSelectedMapPosition()
        {
            Vector3 mousePosition = Input.mousePosition;
            
            // Fix Zoom
            mousePosition.z = _camera.nearClipPlane;

            Ray ray = _camera.ScreenPointToRay(mousePosition);
            RaycastHit hit; 

            // Limit distance --> good performance
            if (Physics.Raycast(ray, out hit, maxDistance: _maxDistance, _placeLayerMask))
            {
                lastPosition = hit.point;
            }
            return lastPosition;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                OnClicked?.Invoke();

            if (Input.GetKeyDown(KeyCode.Escape))
                OnExit?.Invoke();
        }
        
        // Check Touch/Pointer ==> Interact UI.
        public bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();
    }
}