using System;
using Core.Module.Map;
using UnityEngine;
using VContainer;

namespace Core.Module.Farm
{
    /// <summary>
    /// Script hỗ trợ kiểm thử: Tự động bơm Ruộng và Chuồng ảo vào Grid Map lúc Start game.
    /// Kéo vào chung GameObject với FarmInputHandler.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmTestHelper : MonoBehaviour
    {
        private IMapService _mapService;

        [Inject]
        public void Construct(IMapService mapService)
        {
            _mapService = mapService;
        }

        private void Start()
        {
            if (_mapService is MapService concreteMapService)
            {
                try
                {
                    // Dùng Reflection truy cập vào trường _grid private của MapService để mock dữ liệu
                    var gridField = typeof(MapService).GetField("_grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (gridField != null)
                    {
                        var grid = (GridData)gridField.GetValue(concreteMapService);
                        if (grid != null)
                        {
                            // 1. Tạo ô Ruộng ảo (ID 101) kích thước 1x1 tại tọa độ Grid (0, 0, 0)
                            grid.AddObjectAt(new Vector3Int(0, 0, 0), new Vector2Int(1, 1), 101, 999);
                            
                            // 2. Tạo ô Chuồng thú ảo (ID 102) kích thước 2x2 tại tọa độ Grid (3, 0, 3)
                            grid.AddObjectAt(new Vector3Int(3, 0, 3), new Vector2Int(2, 2), 102, 998);

                            Debug.Log("<color=green><b>[FARM TEST HELPER] Đã khởi tạo vùng test ảo thành công!</b></color>\n" +
                                      "- Ô Ruộng đất (1x1) đặt tại tọa độ Grid: (0, 0, 0)\n" +
                                      "- Ô Chuồng nuôi (2x2) đặt tại tọa độ Grid: (3, 0, 3) đến (4, 0, 4)\n" +
                                      "👉 Bạn hãy click vào các tọa độ đất này để test thử tương tác.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FarmTestHelper] Lỗi khi tạo dữ liệu test ảo: {e.Message}");
                }
            }
        }
    }
}
