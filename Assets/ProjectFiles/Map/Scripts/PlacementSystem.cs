using System.Collections.Generic;
using UnityEngine;

namespace Main.Map
{
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _mouseIndicator;
        [SerializeField] private GameObject _cellIndicator;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Grid grid;
        
        [Header("Object Settings")]
        [SerializeField] private ObjectDatabaseSO _objData;
        [SerializeField] private GameObject gridVisualization;
        private int currentObjectIndex = 0;
        
        [Header("Ui Settings")]
        [SerializeField]private Renderer previewRenderer;

        private GridData floorData, furnitureData;
        private List<GameObject> placementObjects;
        
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
            
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            previewRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", Color.white);
            if (!checkValidate)
                propertyBlock.SetColor("_Color", Color.red);
            previewRenderer.SetPropertyBlock(propertyBlock);
            
            // Convert Cell[x,y] --> World Position.
            _cellIndicator.transform.position = grid.CellToWorld(gridPosition);
        }

        private void Start()
        {
            StopPlacement();
            floorData = new GridData();
            furnitureData = new GridData();
            placementObjects = new List<GameObject>();
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
            _cellIndicator.gameObject.SetActive(true);
            _inputManager.OnClicked += PlaceStructure;
            _inputManager.OnExit += StopPlacement;
        }

        public void StopPlacement()
        {
            currentObjectIndex = -1;
            gridVisualization.gameObject.SetActive(false);
            _cellIndicator.gameObject.SetActive(false);
            _inputManager.OnClicked -= PlaceStructure;
            _inputManager.OnExit -= StopPlacement;
        }
        #endregion
        
        #region Logic
        public void PlaceStructure()
        {
            if (_inputManager.IsPointerOverUI())
            {
                return;
            }
            Vector3 mousePostion = _inputManager.GetSelectedMapPosition();
            Vector3Int gridPosition = grid.WorldToCell(mousePostion);
            
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
        }

        private bool CheckPlacementValidate(Vector3Int gridPosition, int selectedObjectIndex)
        {
            return furnitureData.CanPlaceObjectAt(gridPosition, _objData.Objects[selectedObjectIndex].Size);
        }
        #endregion
    }
}