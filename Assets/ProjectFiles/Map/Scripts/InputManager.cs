using System;
using UnityEngine;

namespace Main.Map
{
    public class InputManager : MonoBehaviour
    {
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
            if (Physics.Raycast(ray, out hit, maxDistance: 1000, _placeLayerMask))
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
    }
}