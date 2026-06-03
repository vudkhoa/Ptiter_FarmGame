using UnityEngine;
using UnityEngine.UI;

namespace Core.Module.Map
{
    public class MapPlacer : MonoBehaviour
    {
        [SerializeField] private int _objectId;
        [SerializeField] private Button btn;

        private IMapService _map;

        #region Bind - Manual Injection
        public void Bind(MapService map) 
        {
            _map = map; 
        }

        private void OnEnable()
        {
            btn.onClick.AddListener(Place);
        }

        private void OnDisable()
        {
            btn.onClick.RemoveListener(Place);
        }
        #endregion

        #region Button Hook
        public void Place()
        {
            if (_map == null)
            {
                Debug.LogError($"[MapPlacer] IMapService chưa Bind (objectId={_objectId}). Kiểm tra SelectObjectsScreen.Bind().", this);
                return;
            }

            _map.StartPlacement(_objectId);
        }
        #endregion
    }
}
