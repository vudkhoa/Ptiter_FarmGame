using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Core.Module.Map
{
    public sealed class MapPlacer : MonoBehaviour
    {
        [SerializeField] private int _objectId;
        [FormerlySerializedAs("btn")]
        [SerializeField] private Button _btn;

        private IMapService _map;

        #region Bind - Manual Injection
        public void Bind(MapService map)
        {
            _map = map;
        }

        private void OnEnable()
        {
            if (_btn != null) _btn.onClick.AddListener(Place);
        }

        private void OnDisable()
        {
            if (_btn != null) _btn.onClick.RemoveListener(Place);
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
