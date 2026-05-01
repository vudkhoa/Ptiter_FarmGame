using System.Collections.Generic;
using UnityEngine;

namespace Main.Map
{
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _mouseIndicator;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Grid grid;
        
        [Header("Object Settings")]
        [SerializeField] private ObjectDatabaseSO _objData;
        [SerializeField] private GameObject gridVisualization;

        [Header("PreviewSystem")]
        [SerializeField] PreviewSystem _previewSystem;

        // Runtime
        private int currentObjectIndex = 0;
        private GridData furnitureData;
        private List<GameObject> placementObjects;
        private Vector3Int lastDetectedPosition = Vector3Int.zero;

        #region Unity - LifeCycle;
        private void Update()
        {
            if (currentObjectIndex < 0) return;
            
            Vector3 mousePostion = _inputManager.GetSelectedMapPosition();

            // Get [x,y] Of Cell From Mouse Position
            Vector3Int gridPosition = grid.WorldToCell(mousePostion);
            _mouseIndicator.transform.position = mousePostion;

            // Check Valid
            bool checkValidate = CheckPlacementValidate(gridPosition, currentObjectIndex);

            _previewSystem.UpdatePosition(
                grid.CellToWorld(gridPosition),
                checkValidate);
        }

        private void Start()
        {
            StopPlacement();
            furnitureData = new GridData();
            placementObjects = new List<GameObject>();
            lastDetectedPosition = Vector3Int.zero;
        }
        #endregion

        #region Placement - LifeCycle
        public void StartPlacement(int index)
        {
            StopPlacement();
            // Get Index
            currentObjectIndex = _objData.Objects.FindIndex(x => x.ID == index);
            if (currentObjectIndex < 0)
            {
                Debug.LogError($"Not Found Object {index}");
                return;
            }
            
            // Regis Handle
            gridVisualization.gameObject.SetActive(true);

            _previewSystem.StartShowingPlacementPreview(
                _objData.Objects[currentObjectIndex].Prefab,
                _objData.Objects[currentObjectIndex].Size);

            _inputManager.OnClicked += PlaceStructure;
            _inputManager.OnExit += StopPlacement;
        }

        public void StopPlacement()
        {
            currentObjectIndex = -1;
            gridVisualization.gameObject.SetActive(false);

            _previewSystem.StopShowingPreview();

            _inputManager.OnClicked -= PlaceStructure;
            _inputManager.OnExit -= StopPlacement;

            lastDetectedPosition = Vector3Int.zero;
        }
        #endregion
        
        #region Placement - Logic
        public void PlaceStructure()
        {
            if (_inputManager.IsPointerOverUI())
            {
                return;
            }
            Vector3 mousePostion = _inputManager.GetSelectedMapPosition();
            Vector3Int gridPosition = grid.WorldToCell(mousePostion);
            
            if (gridPosition != lastDetectedPosition)
            {
                bool checkValidate = CheckPlacementValidate(gridPosition, currentObjectIndex);
                if (!checkValidate) return;

                GameObject newObjectPlace = Instantiate(_objData.Objects[currentObjectIndex].Prefab);
                Vector3 newPosition = grid.CellToWorld(gridPosition);
                newPosition.y = 0;
                newObjectPlace.transform.position = newPosition;

                placementObjects.Add(newObjectPlace);
                furnitureData.AddObjectAt(gridPosition, _objData.Objects[currentObjectIndex].Size,
                    _objData.Objects[currentObjectIndex].ID,
                    placementObjects.Count - 1);

                _previewSystem.UpdatePosition(
                    grid.CellToWorld(gridPosition),
                    checkValidate);

                lastDetectedPosition = gridPosition;
            }
        }

        private bool CheckPlacementValidate(Vector3Int gridPosition, int selectedObjectIndex)
        {
            return furnitureData.CanPlaceObjectAt(gridPosition, _objData.Objects[selectedObjectIndex].Size);
        }
        #endregion
    }
}