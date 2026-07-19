using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Common
{
    /// <summary>
    /// Component hỗ trợ di chuyển (Pan) và phóng to/thu nhỏ (Zoom) Camera trong Unity.
    /// Hoạt động tốt cho cả Orthographic (2D/Isometric) và Perspective (3D Top-down).
    /// Hỗ trợ cả điều khiển bằng Chuột (PC) và Cảm ứng đa điểm (Mobile).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPanZoom : MonoBehaviour
    {
        [Header("Camera Reference")]
        private Camera _camera;

        [Header("Pan (Di chuyển Map)")]
        [Tooltip("Phím chuột dùng để di chuyển: 0 = Trái, 1 = Phải, 2 = Giữa")]
        [SerializeField] private int _panMouseButton = 1;
        [SerializeField] private float _panSpeed = 1f;
        [SerializeField] private bool _useDamping = true;
        [SerializeField] private float _dampingFactor = 10f;

        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeedMouse = 10f;
        [SerializeField] private float _zoomSpeedTouch = 0.1f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 20f;

        [Header("Boundaries (Giới hạn di chuyển)")]
        [SerializeField] private bool _useBounds = true;
        [SerializeField] private float _minX = -30f;
        [SerializeField] private float _maxX = 30f;
        [SerializeField] private float _minZ = -30f;
        [SerializeField] private float _maxZ = 30f;

        private Vector3 _targetPosition;
        private float _targetZoom;
        
        private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Vector3 _dragStartWorldPos;
        private bool _isDragging = false;

        // Lưu thông số touch cho mobile
        private Vector2 _touchStartPos;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetPosition = transform.position;
            
            if (_camera.orthographic)
            {
                _targetZoom = _camera.orthographicSize;
            }
            else
            {
                _targetZoom = _camera.fieldOfView;
            }
        }

        private void Update()
        {
            // Kiểm tra xem chuột/tay có đang đè lên UI hay không
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Nếu đang dùng Mobile, kiểm tra Touch(0) xem có đè lên UI ko
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    return;
                }
                
                // Nếu không drag từ trước, bỏ qua input khi con trỏ đè lên UI
                if (!_isDragging) return;
            }

            HandleZoom();
            HandlePan();
        }

        private void LateUpdate()
        {
            ApplyMovement();
        }

        /// <summary>
        /// Xử lý phóng to / thu nhỏ bằng Chuột hoặc Pinch-to-zoom (2 ngón tay)
        /// </summary>
        private void HandleZoom()
        {
            float zoomDelta = 0f;

            // 1. Zoom trên Thiết bị di động (Cảm ứng 2 ngón tay)
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                // Tìm vị trí trước đó của mỗi touch
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                // Tính khoảng cách hiện tại và trước đó
                float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;

                // Độ chênh lệch chính là zoomDelta
                zoomDelta = (currentMagnitude - prevMagnitude) * _zoomSpeedTouch;
                
                // Ngược lại với scrollwheel, khoảng cách tăng nghĩa là zoom-in (giảm size)
                _targetZoom -= zoomDelta;
            }
            // 2. Zoom trên PC (Scroll chuột)
            else
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    zoomDelta = scroll * _zoomSpeedMouse;
                    _targetZoom -= zoomDelta;
                }
            }

            // Giới hạn zoom trong khoảng Min/Max
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
        }

        /// <summary>
        /// Xử lý di chuyển Camera (Pan) bằng cách kéo giữ chuột hoặc kéo 1 ngón tay trên màn hình
        /// </summary>
        private void HandlePan()
        {
            // --- ĐIỀU KHIỂN BẰNG TOUCH (MOBILE) ---
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _isDragging = true;
                    _dragStartWorldPos = GetPlaneIntersectionPoint(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved && _isDragging)
                {
                    Vector3 currentWorldPos = GetPlaneIntersectionPoint(touch.position);
                    // Vector dịch chuyển = Điểm bắt đầu click - Điểm hiện tại dưới ngón tay
                    Vector3 direction = _dragStartWorldPos - currentWorldPos;

                    // Cập nhật vị trí mong muốn của camera
                    _targetPosition += direction;
                    
                    // Do camera đã di chuyển, ta phải cập nhật lại điểm bắt đầu click 
                    // dựa trên vị trí ngón tay hiện tại để tránh việc camera bị trôi quá nhanh.
                    _dragStartWorldPos = GetPlaneIntersectionPoint(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _isDragging = false;
                }
            }
            // --- ĐIỀU KHIỂN BẰNG CHUỘT (PC) ---
            else if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(_panMouseButton))
                {
                    _isDragging = true;
                    _dragStartWorldPos = GetPlaneIntersectionPoint(Input.mousePosition);
                }
                else if (Input.GetMouseButton(_panMouseButton) && _isDragging)
                {
                    Vector3 currentWorldPos = GetPlaneIntersectionPoint(Input.mousePosition);
                    Vector3 direction = _dragStartWorldPos - currentWorldPos;

                    _targetPosition += direction;
                    
                    // Cập nhật lại dragStartWorldPos sau khi đã cộng thêm direction
                    _dragStartWorldPos = GetPlaneIntersectionPoint(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(_panMouseButton))
                {
                    _isDragging = false;
                }
            }
            else
            {
                // Nếu có hơn 2 ngón tay chạm màn hình (đang thực hiện zoom chẳng hạn)
                _isDragging = false;
            }
        }

        /// <summary>
        /// Áp dụng các thay đổi vị trí và zoom của Camera với tính năng làm mượt (Damping/Lerp)
        /// </summary>
        private void ApplyMovement()
        {
            // 1. Áp dụng giới hạn biên (Boundaries) cho targetPosition trước khi di chuyển
            if (_useBounds)
            {
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, _minX, _maxX);
                _targetPosition.z = Mathf.Clamp(_targetPosition.z, _minZ, _maxZ);
            }

            // 2. Làm mượt di chuyển (Position Lerp)
            if (_useDamping)
            {
                transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _dampingFactor);
            }
            else
            {
                transform.position = _targetPosition;
            }

            // 3. Làm mượt phóng to/thu nhỏ (Zoom Lerp)
            if (_camera.orthographic)
            {
                if (_useDamping)
                {
                    _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetZoom, Time.deltaTime * _dampingFactor);
                }
                else
                {
                    _camera.orthographicSize = _targetZoom;
                }
            }
            else
            {
                if (_useDamping)
                {
                    _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetZoom, Time.deltaTime * _dampingFactor);
                }
                else
                {
                    _camera.fieldOfView = _targetZoom;
                }
            }
        }

        /// <summary>
        /// Tính toán toạ độ giao điểm từ tia Raycast (từ màn hình qua Camera) xuống mặt phẳng đất Y = 0.
        /// Đây là thuật toán chuẩn giúp Camera bám sát chuột/ngón tay khi kéo thả.
        /// </summary>
        private Vector3 GetPlaneIntersectionPoint(Vector2 screenPosition)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_groundPlane.Raycast(ray, out float enterDistance))
            {
                return ray.GetPoint(enterDistance);
            }
            
            // Trường hợp dự phòng nếu tia không cắt mặt phẳng nằm ngang
            return transform.position;
        }
    }
}
