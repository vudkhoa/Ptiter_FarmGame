# Nhật ký Phát triển - Hệ thống Farm

---

## Nhật ký ngày: 02-03/07/2026 - Hoàn thành Lõi Backend & Hạ tầng kỹ thuật

### 1. Các đầu việc đã hoàn thành:
- **Part 1 (ScriptableObjects)**: Khai báo cấu trúc mô hình dữ liệu nâng cấp chuyển stage `stage2Threshold` no-code trong `CropData`, `AnimalData`, và `FarmDatabaseSO`. Expose các danh sách để UI truy vấn.
- **Part 2 (Serialization & Lưu trữ)**: Tạo mô hình dữ liệu lưu trữ FSM `FarmSlotSaveData` tại module `Farm`. Mở rộng `PlayerData` (tiền xu `Coins`, cấu trúc túi đồ `InventoryEntry` kiểu struct chống GC, danh sách ô đất và cờ hack).
- **Part 3 (Hạ tầng FSM & Chống tua giờ)**:
  - Khai báo assembly `Farm.asmdef` độc lập của module (sau này được gán GUID và các tham chiếu).
  - Tạo interface `IFarmInventoryProvider` cho thiết kế đảo ngược phụ thuộc (DIP), ngắt hoàn toàn dependency chéo lên gameplay chính.
  - Viết `FarmService` điều phối FSM, tăng trưởng qua ClockTick (nhận nhịp tick 1 giây từ ClockService) và tính toán offline progress lúc mở game.
  - Tạo sự kiện MessagePipe `FarmSlotChangedPayload` để thông báo cho View hiển thị.
  - Viết `WebTimeSyncSource` đồng bộ giờ chuẩn UTC máy chủ, có chế độ fallback chạy offline.
  - Thêm logic kiểm tra tua giờ thời gian thực trong `ClockService` (so sánh nhịp CPU unscaled) và kiểm tra tua giờ offline trong `ServerTimeService` (so sánh chênh lệch Offset).
  - Khai báo DI VContainer và MessagePipe trong `RootLifetimeScope`.

### 2. Các quyết định thiết kế chính & Cơ sở lý thuyết:
- **Quyết định sử dụng Struct vs Class (`InventoryEntry` vs `FarmSlotSaveData`)**:
  - `InventoryEntry` dùng **`struct`** vì nó là dữ liệu thô dạng Key-Value cực nhẹ (ID, số lượng). Struct thuộc kiểu tham trị (Value Type), được cấp phát trực tiếp trên Stack nên **hoàn toàn không sinh rác GC** khi thay đổi số lượng, tối ưu hóa tối đa hiệu suất cho Mobile.
  - `FarmSlotSaveData` dùng **`class`** vì nó là dữ liệu biến đổi liên tục (Mutable State) mang tính chất thực thể (Entity). Nếu dùng struct, cơ chế sao chép giá trị của C# sẽ làm việc chỉnh sửa thời gian sinh trưởng chỉ tác động lên bản sao của ô ruộng chứ không lưu ngược lại danh sách. Lớp `PlayerData.FarmSlots` chia sẻ trực tiếp tham chiếu (reference sharing) với `FarmService` giúp tự động đồng bộ khi lưu game.
- **Thiết kế Đảo ngược Phụ thuộc (DIP) lúc khởi đầu**:
  - Tạo giao diện `IFarmInventoryProvider` trong assembly `Farm` để `PlayerDataHolder` trong `MyOwn` kế thừa, giải quyết bài toán circular dependency ban đầu giữa hai assembly chính.
- **Cơ chế Chống Cheat Đa Tầng**:
  - **Lớp 1 (Real-time check)**: Chạy liên tục mỗi giây trong `ClockService`. So sánh tiến trình đồng hồ hệ thống thiết bị với đồng hồ phần cứng bất biến của vi xử lý CPU (`Time.unscaledTime`). Bắt hack lập tức khi người chơi chỉnh giờ khi đang mở game.
  - **Lớp 2 (Offline check)**: Chạy khi khởi động game hoặc khi có mạng lại. So sánh Offset thời gian mạng thực tế với Offset cũ. Bắt hack khi người chơi tắt game chỉnh giờ rồi mới mở lại.

---

## Nhật ký ngày: 03-04/07/2026 - Tách rời Module Storage & Hoàn thiện Contract dùng chung

### 1. Các đầu việc đã hoàn thành:
- **Tạo cấu trúc Module `Storage` mới** (Người dùng tự quản lý file `.asmdef`):
  - [IStorageService.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/Module/Storage/Scripts/Service/IStorageService.cs) [NEW]: Interface dùng chung cho Kho và Tiền xu. Thay thế cho giao diện nông nghiệp chuyên biệt `IFarmInventoryProvider.cs` cũ.
  - [InventoryChangedPayload.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/Module/Storage/Scripts/Payloads/InventoryChangedPayload.cs) [NEW]: Payload chứa thông số Item đổi số lượng (`ItemId`, `NewAmount`, `Delta`), phục vụ phát đi qua MessagePipe để UI/Quest tự động lắng nghe và cập nhật.
- **Xóa bỏ interface cũ**:
  - Đã xóa tệp tin `IFarmInventoryProvider.cs` cũ của Farm để dọn sạch code.
- **Đồng bộ hóa trong `FarmService`**:
  - [FarmService.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/Module/Farm/Scripts/Service/FarmService.cs): Chuyển đổi toàn bộ tham chiếu cũ sang sử dụng `IStorageService` mới.
- **Đồng bộ hóa trong `PlayerDataHolder` (Gameplay chính)**:
  - [PlayerDataHolder.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/myOwn/Scripts/Data/PlayerDataHolder.cs): Kế thừa `IStorageService` và thực thi các phương thức.
  - Tích hợp phát sự kiện `InventoryChangedPayload` bên trong hàm `AddItem()` và `RemoveItem()`.
- **Cập nhật VContainer & MessagePipe**:
  - [RootLifetimeScope.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/myOwn/Scripts/Bootstrap/RootLifetimeScope.cs): Đăng ký Message Broker cho `InventoryChangedPayload`.
- **Cập nhật Unit Test FSM**:
  - [FarmFsmTests.cs](file:///d:/Repositories/Ptiter_FarmGame/Assets/Module/Farm/Tests/FarmFsmTests.cs): Cập nhật mock class `MockInventoryProvider` kế thừa `IStorageService` để biên dịch thành công.

### 2. Các quyết định thiết kế chính & Cơ sở lý thuyết:
- **Lợi ích của Module Storage độc lập**:
  - `Farm` và `Cooking` đều cần tương tác với Kho/Tiền tệ nhưng lại là các module gameplay độc lập không được phụ thuộc chéo vào nhau.
  - Module `Storage` đóng vai trò là "Leaf Node" (Nút lá) ở tầng thấp hơn. Cả hai module `Farm` và `Cooking` đều tham chiếu xuống `Storage`, còn `Storage` không cần biết gì về các module trên.
  - `MyOwn` (Assembly chính cao nhất) tham chiếu tới cả 3 để nối ghép DI thông qua VContainer.

### 3. Yêu cầu Cấu hình Assembly `.asmdef` (Người dùng tự thực hiện)

Dưới đây là các tham chiếu bạn cần cấu hình bằng tay trong Unity Editor:

1. **Tạo `Storage.asmdef`** tại `Assets/Module/Storage/Scripts/`:
   - Thêm các Assembly References (GUIDs):
     * `UniTask` (`GUID:08b38f39e2d9e594389b7a4cf4c2c338`)
     * `MessagePipe` (`GUID:4f682e06dbb3e624faedad9cc27106cc`)
     * `MessagePipe.VContainer` (`GUID:b0214a6008ed146ff8f122a6a9c2f6cc`)
     * `VContainer` (`GUID:f51ebe6a0ceec4240a699833d6309b23`)

2. **Cập nhật `Farm.asmdef`** tại `Assets/Module/Farm/Scripts/`:
   - Thêm Assembly Reference tới:
     * `Storage` (Assembly mới tạo)

3. **Cập nhật `MyOwn.asmdef`** tại `Assets/myOwn/Scripts/`:
   - Thêm Assembly Reference tới:
     * `Storage` (Assembly mới tạo)

4. **Cập nhật `Farm.Tests.asmdef`** tại `Assets/Module/Farm/Tests/`:
   - Thêm Assembly Reference tới:
     * `Storage` (Assembly mới tạo)

---

## Nhật ký ngày: 04/07/2026 - Hoàn thiện Grid Queries, Input Handling, UI & Hiển thị Trực quan và Tối ưu Hiệu năng

### 1. Các đầu việc đã hoàn thành:
- **Part 4 (Grid Queries & Input Handling)**:
  - Bổ sung `TryGetPlacementAt` và `WorldToCell` vào `IMapService` và `MapService`.
  - Tạo payload `OpenFarmSelectorUIPayload` mở bảng chọn hạt.
  - Viết `FarmInputHandler` MonoBehaviour bắt raycast click thông minh (một chạm gieo/cho ăn/gặt) và giải quyết tọa độ gốc (Pivot) của chuồng trại kích thước lớn ($2 \times 2, 3 \times 3$) tránh lỗi phân mảnh.
- **Part 5 (UI and Visualization)**:
  - Viết `FarmSlotView` hiển thị Morphing Sprite 3 giai đoạn lớn lên của lúa/thú, thanh Slider tiến trình và bong bóng đòi ăn/đòi gặt. Hỗ trợ Billboard xoay song song mặt phẳng camera.
  - Viết `FarmVisualizer` sinh/diệt các đối tượng `FarmSlotView` trên Grid bản đồ theo thời gian thực.
  - Viết `FarmSeedSelectorUI` (WindowController) trong `Assembly-CSharp` để tích hợp hoàn hảo với hệ thống **Bruno Mikoski's UI Manager** của dự án.
  - Tạo `FarmUIBridge` cầu nối lắng nghe sự kiện click mở UI trung gian.
- **Mock/Debug Helpers**:
  - Viết `FarmDebugLogger` in log màu Console.
  - Viết `FarmTestHelper` tự động sinh 1 ruộng ảo tại `(0,0,0)` và 1 chuồng gà ảo tại `(3,0,3)` để test nhanh khi chạy PlayMode.
- **Refactoring & Performance Optimization**:
  - **Tối ưu hóa ghi save trễ (Throttled/Async Save)**: Cập nhật `PlayerDataHolder.cs` trì hoãn ghi ổ cứng 1 giây và đẩy tác vụ xuống Thread Pool chạy bất đồng bộ qua UniTask, tránh giật hình (stuttering) khi click gặt nhanh liên tục. Gọi ghi đè đồng bộ lập tức ở `Dispose()` để chống mất save.
  - **Cơ chế giữ nguyên thú lớn (Permanent Adulthood)**: Bổ sung cờ `isAdult` vào `FarmSlotSaveData`. Thú lớn lên 1 lần duy nhất từ con non ở chu kỳ đầu, các chu kỳ tiếp theo sau khi thu hoạch trứng sẽ giữ nguyên hình dạng lớn (`growthSprites[1]`) đi lại đòi ăn và đẻ tiếp, không bị thu nhỏ lại.
  - **Sử dụng biên dịch có điều kiện**: Bao bọc toàn bộ code của `FarmTestHelper` và `FarmDebugLogger` cùng phần DI trong `GameLifetimeScope.cs` bằng thẻ `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

### 2. Các quyết định thiết kế chính & Cơ sở lý thuyết:
- **Tương tác một chạm thông minh (Context-Aware Click)**: Trải nghiệm người dùng di động đòi hỏi thao tác tối giản. Click ô đất trống $\rightarrow$ mở UI chọn hạt giống, click gà đói $\rightarrow$ cho gà ăn trực tiếp, click lúa chín $\rightarrow$ thu hoạch ngay. FSM tự động điều phối mà không cần mở popup trung gian rườm rà.
- **Giải quyết giới hạn thứ tự biên dịch (Assembly Compilation Limits)**: Do các script UI của UIManager nằm ở `Assembly-CSharp`, còn DI container `GameLifetimeScope` nằm ở `MyOwn.asmdef`, nên chúng ta không thể import tĩnh UI vào `GameLifetimeScope`. 
  - *Giải pháp*: Xóa bỏ đăng ký tĩnh trong code và đưa UI vào danh sách `autoInjectGameObjects` của Scope, sau đó gọi `_resolver.Inject(screen)` động khi UIManager vừa Instantiate nó lên RAM.
- **Tối ưu hóa I/O thắt nút cổ chai (I/O Bottleneck)**: Throttling & Thread Pool là các mẫu thiết kế chuẩn cho việc lưu trữ save trên Mobile, giúp giải phóng hoàn toàn Main Thread khỏi việc ghi đĩa đồng bộ chặn CPU.

