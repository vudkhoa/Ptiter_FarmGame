# GAME DESIGN DOCUMENT — BUÔN VILLAGE
### Farming / Life Simulation 2.5D · Mobile-first (Unity 3D URP)
### BẢN THẢO ĐỀ XUẤT DỰ ÁN THỰC TẬP — Tháng 06/2026

**Đơn vị:** Học viện Công nghệ Bưu chính Viễn thông — Cơ sở TP. Hồ Chí Minh — Khoa Công nghệ Thông tin 2
**Tác giả:** Vũ Đình Khoa — PO / Lead Dev / Game Designer

**Thành viên Công nghệ Thông tin:**
- Vũ Đình Khoa — N22DCCN142 — D22CQCNPM02-N
- Phạm Hùng Thiên — N22DCCN180 — D22CQCNPM02-N
- Văn Minh Tấn — N22DCCN175 — D22CQCNPM02-N

**Thành viên khác:** Đội Artist ngành Đa phương tiện

---

## GHI CHÚ NHÂN SỰ DỰ ÁN
- **Leader:** Vũ Đình Khoa — đảm nhận vai trò Product Owner, Lead Developer và Designer.
- **Thành viên:**
  - Phạm Hùng Thiên: Developer, Game Design.
  - Văn Minh Tấn: Developer.
- **Mảng Art:** 03 sinh viên ngành Đa phương tiện đảm nhận.

---

## 1. TÓM TẮT

> **Buôn Village** — game *Farming / Life Sim* **mobile-first**, bối cảnh **buôn làng Tây Nguyên**, kết hợp vòng lặp gây nghiện kiểu *Dreamdale* với chất nghệ thuật vẽ tay sử thi kiểu *Don't Starve*.

- **Bản sắc độc bản:** Chưa game farming nào khai thác văn hoá Tây Nguyên → khác biệt rõ trên thị trường.
- **Khả thi kỹ thuật (trọng tâm):** Đã có **04 module nền** dựng theo **kiến trúc Service + DI (VContainer) + ScriptableObject** đang vận hành (Input, Map, Time, UI) → MVP nằm trong tầm 8 tuần.
- **Mục tiêu thực tập:** Demo dọc (vertical slice) chạy trọn **core gameplay loop** trên mobile, đủ trình diễn & bảo vệ, **publish CHPlay nếu có tiềm năng**.

---

## 2. TỔNG QUAN DỰ ÁN

| Thuộc tính | Giá trị |
|---|---|
| Tên dự án | **Buôn Village** |
| Thể loại | Farming + Life Simulation + Adventure RPG nhẹ |
| Reference | **Dreamdale** (one-tap, land expansion) · **Don't Starve** (vẽ tay, isometric) |
| Engine nền | **Unity 3D (URP)** |
| Phong cách hình ảnh | **2.5D** isometric (camera góc cố định, giả lập chiều sâu trên không gian 3D) |
| Nền tảng | **Mobile (chính)** · sẽ update thêm PC nếu có tiềm năng |
| USP | Bản sắc Tây Nguyên · cơ chế một-chạm gây nghiện · âm nhạc cồng chiêng |

---

## 3. VÒNG LẶP GAMEPLAY CỐT LÕI

```
[Di chuyển + Thu thập một-chạm: Gỗ / Đá / Cà phê]
                 │
                 ▼
[Bán thô  HOẶC  Chế biến sâu → Đặc sản (giá X3–X5)]
                 │
                 ▼
[Tích luỹ Tiền tệ + Danh tiếng Buôn làng]
                 │
                 ▼
[Nâng cấp Nhà Dài + Mở rộng Nông trại]
                 │
                 └──> Pet/Gia súc rút ngắn thời gian + buff cơ động
```
*Sơ đồ 1 — Vòng lặp gameplay cốt lõi. Triết lý: vòng lặp ngắn, phần thưởng tức thời (dopamine loop), giảm ma sát thao tác cho mobile.*

---

## 4. THIẾT KẾ HỆ THỐNG GAMEPLAY

### 4.1. Điều khiển & Nhân vật — Mobile-first
- **Mobile (chính):** Joystick ảo một-chạm — *Hold to Move & Auto-Action*; thao tác tối giản cho cảm ứng.
- **PC (phụ):** WASD + chuột — chỉ phục vụ kiểm thử & phát triển.
- **Context-Aware:** Không cần đổi công cụ — cạnh cây → tự vung rìu; cạnh cà phê chín → tự tuốt vào gùi.
- **Giới hạn gùi:** Sức chứa có hạn → tạo nhịp di chuyển có chủ đích.

### 4.2. Mở rộng Bản đồ (Land Expansion) — MVP rút gọn
- **MVP chỉ làm:** khu **Nông trại gia đình** (nhà dài cấp 1, giếng cổ, ô rẫy bazan).
- **Kết mở (open ending):** khi đạt điều kiện, phát **cutscene mở ra Buôn làng** như lời hứa hẹn cho người chơi.
- **Tương lai:** mở rộng đầy đủ các phân khu (Buôn làng/Hub, Rừng Già, Bến Nước) chỉ triển khai khi dự án ổn định & có tiềm năng.

### 4.3. Trồng trọt (Farming)
Tinh giản: tiếp cận ô rẫy → tự gieo → chờ chín real-time (UTC) → đi qua thu hoạch.

| Cây trồng | Thời gian chín | Sản phẩm | Đặc tính |
|---|---|---|---|
| Lúa Rẫy | 60s | Hạt lúa thô | Không tưới, thu 1 lần |
| Cà Phê Robusta | 180s | Quả cà phê chín | Lâu năm, thu nhiều đợt (90s) |
| Hồ Tiêu | 240s | Chuỗi tiêu xanh | Cần cọc gỗ để leo (5 gỗ) |

### 4.4. Chăn nuôi
| Con vật | Cơ chế |
|---|---|
| Gà Rừng | Ăn lúa thô → đẻ Trứng mỗi 45s; đi ngang để nhặt |
| Heo Sọc Dưa | Ăn Sắn/Khoai → Thịt Heo Gác Bếp (qua lò sấy) |
| Trâu Bản Địa | +50% tốc độ & sức chứa gùi trong nông trại (buff cơ động) |

### 4.5. Chế biến sâu (Crafting Pipeline)
```
Cà Phê Thô → [Sân Phơi] → Nhân Xanh → [Lò Rang Đất Sét] → Cà Phê Rang Củi  (X5)
Tiêu Xanh  → [Gùi Sấy Khô] → Hồ Tiêu Đen Bao Thổ Cẩm                       (X3)
```

### 4.6. Xây dựng & Tiến trình (Progression)
| Cấp Nhà Dài | Yêu cầu | Mở khoá |
|---|---|---|
| Cấp 1 — Nhà Sàn Nhỏ | Mặc định | Kho cơ bản (50 tài nguyên) |
| Cấp 2 — Mở Rộng | 100 Gỗ + 50 Đá | NPC Thương lái; +4 ô rẫy |
| Cấp 3 — Đại Bản Doanh | 300 Gỗ Quý + 150 Cà phê Rang | Kích hoạt cutscene mở bản làng (kết mở) |

### 4.7. NPC & Nhiệm vụ
- **Già Làng Ama Thuột** — Main Quest (yêu cầu tài nguyên sửa công trình).
- **Nghệ nhân Y-Khuê** — Side Quest (cung cấp nông sản chuẩn → bản vẽ nâng cấp).
- **Thương Lái Phương Xa** — thu mua nông sản chế biến, giá biến động theo ngày.

### 4.8. UI/UX & Audio
- **UI:** triển khai theo **UICollection** (xem mục 5.4); panel vân gỗ-dây thừng; thanh trạng thái dạng gùi đầy nước/củi cháy; icon vẽ tay thổ cẩm.
- **Audio:** ve sầu/gió tre/thác nước; SFX "cộc cộc", "xoàn xoạt"; BGM đàn T'rưng + cồng chiêng + trống da trâu.

---

## 5. KIẾN TRÚC KỸ THUẬT & TÍNH KHẢ THI (★ TRỌNG TÂM DEV)

Phần trọng tâm: dự án không chỉ là ý tưởng mà đã có nền móng mã nguồn vận hành.

### 5.1. Nguyên tắc kiến trúc
Kiến trúc **Service-based · decoupled qua event · data-driven (ScriptableObject)** — mỗi hệ thống là module độc lập, dễ kiểm thử & mở rộng.
- **Service pattern:** mỗi hệ thống lớn là 1 Service, giao tiếp qua interface.
- **Event-driven:** module phát/nghe **Payload events** → không phụ thuộc chéo.
- **Data-driven:** cấu hình nằm trong ScriptableObject → chỉnh không cần code.

### 5.2. Vì sao dùng VContainer thay vì tự setup DI thủ công?

| Tiêu chí | Tự setup thủ công (Singleton / Service Locator) | **VContainer (đã chọn)** |
|---|---|---|
| Quản lý vòng đời | Tự viết, dễ rò rỉ & sai thứ tự khởi tạo | **LifetimeScope** quản lý vòng đời rõ ràng |
| Phụ thuộc | Ẩn, khó nhìn ra ai cần gì | Khai báo tường minh qua `[Inject]` |
| Kiểm thử / mock | Khó thay implementation | Dễ swap interface → unit test thuận lợi |
| Boilerplate | Nhiều code khởi tạo lặp lại | Container tự resolve, giảm boilerplate |
| Hiệu năng | — | **Hiệu năng cao, ít GC alloc** (tối ưu mobile) |

→ **Kết luận:** VContainer giữ kiến trúc Service **decoupled & testable** mà không phải tự "phát minh lại bánh xe"; đặc biệt phù hợp mobile nhờ chi phí cấp phát thấp.

### 5.3. Các module nền đã có (Proof of Feasibility)

| Module mã nguồn | Phục vụ hệ thống | Trạng thái |
|---|---|---|
| `Module/Input` | Điều khiển context-aware (4.1) | ✅ Đã chạy — event-driven |
| `Module/Map` | Land Expansion (4.2), Xây dựng (4.6) | ✅ Đã chạy — grid + preview |
| `Module/Time` | Farming (4.3), Chế biến (4.5) | ✅ Đã chạy — clock + sync + chống chỉnh giờ |
| `Module/UI System` | UI/UX (4.8) | 🔨 Đang dựng — theo UICollection |
| `Module/DesignPattern` | Hạ tầng chung (MonoSingleton) | ✅ |

### 5.4. UI System — triển khai theo UICollection
UI tổ chức theo **collection của Layer & Group** (`UILayerCollection`, `UIGroupCollection`): mỗi màn hình/popup gán vào Layer (Main / Overlay / Popup) và Group → quản lý thứ tự hiển thị, vòng đời mở/đóng tập trung, dễ thêm màn hình mới mà không sửa code lõi.

### 5.5. Backend & Lưu trữ — Firebase
- **Hiện tại:** dùng **Firebase** cho **Authentication + Save/Load** (cloud save), tận dụng SDK & hạ tầng sẵn có → tiết kiệm thời gian, ổn định.
- **Chống chỉnh giờ:** tận dụng **server timestamp của Firebase** làm nguồn thời gian tham chiếu (kết hợp `ClockService` đã có cờ phát hiện chỉnh giờ).
- **Tương lai:** nếu dự án phát triển, xây **hệ thống Backend riêng** (authoritative time, anti-cheat, leaderboard).

### 5.6. Luồng dữ liệu tiêu biểu (ví dụ: thu hoạch)
```
InputService (chạm) ──payload──> Player Controller ──> Farming Service
Farming Service nghe ClockService.ClockTick ──> kiểm tra cây chín
Cây chín ──> thêm vào Inventory ──> bắn event UI cập nhật gùi
```
*Sơ đồ 2 — Luồng dữ liệu giữa các Service (event-driven).*

### 5.7. Công nghệ sử dụng (Tech Stack)
| Hạng mục | Lựa chọn | Lý do |
|---|---|---|
| Engine | **Unity 3D (URP)** | Nền 3D, trình bày 2.5D; build mobile tốt |
| DI Container | **VContainer** | Decoupled, testable, ít GC (mục 5.2) |
| Dữ liệu cấu hình | ScriptableObject | Tách cấu hình khỏi code |
| Backend / Save | **Firebase** | Auth + Cloud Save sẵn sàng (mục 5.5) |
| UI | UICollection (Layer/Group) | Quản lý màn hình tập trung (mục 5.4) |
| Source control | Git (branch theo feature) | — |

---

## 6. PHẠM VI MVP (SCOPE CONTROL)

**✅ TRONG SCOPE — Demo dọc 8 tuần:**
- Core loop: di chuyển + thu thập context-aware + bán (+ hệ thống Map nông trại).
- Farming (3 cây) + Chăn nuôi (gà/heo/trâu).
- Chế biến cà phê + Progression Nhà Dài (cấp 1→3).
- NPC cơ bản (Già Làng quest + Thương lái bán).
- **Cutscene mở bản làng** (kết mở) — thay cho mini-game.
- UI theo UICollection + Save/Load qua Firebase + audio cốt lõi.

**❌ NGOÀI SCOPE — hướng phát triển tương lai:**
- **Mini-game Lễ hội Cồng chiêng (rhythm)** — đẩy sang tương lai.
- Mở rộng đầy đủ bản đồ (Buôn làng/Hub, Rừng Già, Bến Nước), câu cá, bẫy tôm.
- Backend riêng (authoritative time / anti-cheat server), leaderboard.
- Đa ngôn ngữ, monetization.

---

## 7. QUẢN TRỊ DỰ ÁN

### 7.1. Đối tượng & Thị trường
Game thủ casual mobile thích farming/idle (16–35), ưa thẩm mỹ indie/hand-drawn. Phân khúc **cosy farming sim** đang tăng (Stardew, Dreamdale, Cozy Grove). Lợi thế: ngách văn hoá Tây Nguyên chưa đối thủ chiếm.

### 7.2. Tiến độ — mô hình Scrum, 4 Sprint × 2 tuần (từ 02/06/2026)

> Làm theo **Scrum**: mỗi Sprint 2 tuần có *Sprint Goal*, *Sprint Backlog* (task chia nhỏ), kết Sprint có *Review* (build chạy được) + *Retrospective*.

| Sprint | Thời gian | Sprint Goal | Backlog chính (bàn giao) |
|---|---|---|---|
| **Sprint 1** | 02/06 – 15/06 | **Core Loop + Map** | Di chuyển + context-action + thu thập + inventory gùi + bán cơ bản; **hoàn thiện hệ thống Map nông trại (grid/đặt object)** |
| **Sprint 2** | 16/06 – 29/06 | **Farming + Chăn nuôi** | 3 cây real-time qua ClockService; thu hoạch; gà/heo/trâu sản xuất tự động |
| **Sprint 3** | 30/06 – 13/07 | **Chế biến + Progression + NPC + Firebase** | Pipeline cà phê; Nhà Dài 1→3; NPC quest + bán; Save/Load Firebase |
| **Sprint 4** | 14/07 – 27/07 | **Cutscene + UI + Polish + Demo** | Cutscene mở bản làng; UI theo UICollection; audio, hiệu ứng, fix bug; build demo |

### 7.3. Tiêu chí Demo Thành công (KPI)
1. ✅ Hoàn thành trọn **1 vòng core loop** (thu thập → chế biến → bán → nâng cấp) không lỗi chặn.
2. ✅ **Trồng trọt đầy đủ:** ≥ 3 cây trồng chín real-time, thu hoạch đúng, chuỗi trồng → thu → chế biến → bán hoạt động trọn vẹn.
3. ✅ Ít nhất **1 loại vật nuôi** sản xuất tự động đúng chu kỳ.
4. ✅ Nâng cấp Nhà Dài tới **cấp 3** → kích hoạt **cutscene mở bản làng**.
5. ✅ **Save/Load qua Firebase** hoạt động (thoát & vào lại giữ tiến trình).
6. ✅ Build chạy ổn định trên **mobile (60 FPS)**.

### 7.4. Rủi ro & Phương án Dự phòng
| Rủi ro | Mức | Phương án |
|---|---|---|
| **Quỹ thời gian hạn chế** (thành viên đi làm song song) | Cao | **Break task nhỏ vào Product Backlog**; làm bản task breakdown chi tiết nhất theo từng Sprint; ưu tiên KPI |
| Art hand-drawn 2.5D tốn thời gian | TB | 3 SV Đa phương tiện phụ trách song song; placeholder + asset free giai đoạn đầu |
| Phình phạm vi (scope creep) | Cao | Bám mục 6; mini-game & mở rộng bản đồ → backlog tương lai |
| Cân bằng số liệu | TB | Số liệu trong ScriptableObject → tinh chỉnh nóng |
| Tích hợp Firebase | Thấp | SDK & tài liệu sẵn; bọc qua service interface để dễ thay/mở rộng |

---

## 8. HƯỚNG MỞ RỘNG (POST-MVP)
- **Mini-game Lễ hội Cồng chiêng** (rhythm) + đa lễ hội (đâm trâu, mừng lúa mới).
- Mở rộng đầy đủ bản đồ: Buôn làng/Hub, Rừng Già, Bến Nước; câu cá, bẫy tôm.
- Kinh tế động (Chợ Buôn giá cung–cầu); thời tiết, mùa vụ.
- Backend riêng: authoritative time, anti-cheat, bảng xếp hạng buôn làng.
- Thương mại hoá: cosmetic thổ cẩm, không pay-to-win.

---

*Tài liệu tạo ngày 02/06/2026 — Vũ Đình Khoa (PO / Lead Dev / Game Design).*
