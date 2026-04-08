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

        private void Update()
        {
            Vector3 mousePostion = _inputManager.GetSelectedMapPosition();

            // Get [x,y] Of Cell From Mouse Position
            Vector3Int gridPosition = grid.WorldToCell(mousePostion);
            _mouseIndicator.transform.position = mousePostion;

            // Convert Cell[x,y] --> World Position.
            _cellIndicator.transform.position = grid.CellToWorld(gridPosition);
        }
    }
}