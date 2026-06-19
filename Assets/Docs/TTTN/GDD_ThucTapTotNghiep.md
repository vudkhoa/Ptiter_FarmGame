# GAME DESIGN DOCUMENT — THỰC TẬP TỐT NGHIỆP

> **[ TÊN GAME — ĐANG CẬP NHẬT ]**
> Game Farming + Cooking Simulation · Mobile (Landscape) · 60FPS · Offline/Online
> Chủ đề: **Đồ ăn Việt Nam** — Từ nông trại đến mâm cơm Việt

**Nhân sự:** Vũ Đình Khoa (PO/Lead Dev/GD) · Phạm Hùng Thiên (Dev/GD) · Văn Minh Tấn (Dev) · 03 SV Đa phương tiện (Art)

---

## 1. TÓM TẮT
Người chơi **trồng trọt – chăn nuôi** để lấy nguyên liệu, rồi **chế biến thành món ăn Việt** (bánh mì, phở, cơm tấm, bánh chưng…). Mỗi món hoàn thành lần đầu mở **cutscene kể chuyện** gắn nhân vật nổi tiếng.

- **Bản sắc độc bản:** chưa game farming nào hệ thống hoá ẩm thực Việt → khác biệt + quảng bá văn hoá.
- **Tâm lý cốt lõi:** liên tục unlock nguyên liệu & món **MỚI** → đánh vào tâm lý thích khám phá.
- **Khả thi kỹ thuật:** đã có 04 module nền (Service + DI VContainer + ScriptableObject); Map dùng Unity Tilemap; Time chống cheat qua Firebase → MVP khả thi 8 tuần.

---

## 2. TỔNG QUAN DỰ ÁN
| Thuộc tính | Giá trị |
|---|---|
| Thể loại | Farming + Cooking Simulation + Light RPG |
| Reference | Dreamdale (one-tap) · Cooking/Diner games · Stardew |
| Engine | Unity (URP) |
| Hướng màn hình | Ngang (Landscape), khoá ngang — tối ưu 60 FPS |
| Chế độ chơi | Offline + Online (cloud save, event, chống cheat thời gian) |
| Phong cách | 2.5D / 2D Tilemap, vẽ tay |
| Nền tảng | Mobile — Android trước, iOS sau |
| USP | Đồ ăn Việt · unlock liên tục · cutscene gắn nhân vật nổi tiếng |

---

## 3. VÒNG LẶP GAMEPLAY CỐT LÕI (CORE LOOP)
```
[Chọn món muốn làm]  (từ Unlock / Quest)
        → [Xem checklist nguyên liệu]
        → [Trồng trọt / Chăn nuôi]  --theo Time System-->  [Thu hoạch]
        → [Sản xuất / Nấu ăn]  (Cooking + Animation)
        → [Hoàn thành món]  →  [CutScene câu chuyện món ăn]
        → [Coins + Danh tiếng]  →  [Unlock nguyên liệu / món MỚI]  (lặp lại)
```
**Triết lý:** vòng lặp ngắn, thưởng tức thời (coins + món mới + cutscene), luôn hé lộ nội dung kế tiếp.

---

## 4. THIẾT KẾ HỆ THỐNG GAMEPLAY
- **4.1 Điều khiển & Camera (landscape):** one-tap context-aware, drag pan map, pinch-zoom, khoá ngang 60FPS.
- **4.2 Map System (Tilemap):** 1 map, config được, trồng lên ô tile, kéo (drag) map có clamp biên.
- **4.3 Unlock System:** 2 nhánh — Unlock Nguyên liệu & Unlock Món (recipe có checklist nguyên liệu), mở bằng **coins**; teaser nội dung kế tiếp.
- **4.4 Trồng trọt & Chăn nuôi (Farm):** theo Time System; mỗi nguyên liệu có **3 giai đoạn sinh trưởng** (giống → phát triển → trưởng thành).
- **4.5 Nấu ăn (Cooking):** trạm chế biến + **animation**; hàng đợi; thời gian theo Time System (tăng tốc qua Booster).
- **4.6 Quest System:** unlock món → quest ép làm món đó N lần; gồm Tutorial / Main / Daily quest.
- **4.7 Event System:** lifecycle chung; config no-code (ScriptableObject); **event đầu tiên: "Tết Việt – Gói Bánh Chưng"**.
- **4.8 Tutorial:** dùng món thân thuộc **Bánh Mì**; dẫn người mới qua core loop < 3 phút.
- **4.9 Shop & Booster:** x2 tốc độ trồng · x2 tài nguyên · giảm thời gian nấu · mở rộng kho (mua bằng coins).
- **4.10 Cutscene sáng tạo:** món signature → câu chuyện gắn nhân vật nổi tiếng (dùng nhân vật gốc/được cấp phép).
- **4.11 UI/UX & Audio:** layout ngang, checklist trực quan; SFX nấu (xèo, lóc bóc), BGM dân gian.
- **4.12 Update System (Nâng cấp công trình):** xây & nâng cấp **công trình trong farm** bằng coins + nguyên liệu → đổi hình theo cấp + buff.

### Bảng món ăn (Unlock + Recipe)
| Món | Checklist nguyên liệu | Unlock | Bán |
|---|---|---|---|
| Bánh Mì (tutorial) | Bột + Thịt + Rau + Trứng | Miễn phí | 30 |
| Cơm Tấm | Gạo + Thịt heo + Trứng + Rau | 300 | 80 |
| Phở Bò | Bánh phở + Thịt bò + Hành + Rau | 600 | 150 |
| Gỏi Cuốn | Bánh tráng + Tôm + Thịt + Rau | 800 | 180 |
| Bún Chả | Bún + Thịt heo + Rau + Nước mắm | 1000 | 220 |
| Bánh Chưng (Event Tết) | Gạo nếp + Đậu xanh + Thịt heo + Lá dong | Qua Event | 300 |

### Bảng nguyên liệu
| Cây trồng | Thời gian | Giai đoạn | Sản phẩm |
|---|---|---|---|
| Rau thơm | 30s | 3 | Rau |
| Lúa | 60s | 3 | Gạo |
| Lúa Nếp | 90s | 3 | Gạo nếp |
| Đậu Xanh | 120s | 3 | Đậu xanh |
| Mía | 180s (nhiều đợt) | 3 | Đường |

| Vật nuôi | Cơ chế | Sản phẩm |
|---|---|---|
| Gà | Ăn thóc → đẻ trứng 45s | Trứng / Thịt gà |
| Heo | Ăn rau/sắn → tới chu kỳ | Thịt heo |
| Bò | Nuôi dài hơn → giá cao | Thịt bò (phở) |

### Bảng công trình nâng cấp (Update System)
| Công trình | Hiệu ứng nâng cấp (cấp 1 → N) |
|---|---|
| Bếp / Trạm nấu | Nấu nhanh hơn + thêm slot + mở món cao cấp |
| Kho | Tăng sức chứa nguyên liệu & thành phẩm |
| Chuồng trại | Tăng số vật nuôi & tốc độ sản xuất |
| Vùng trồng (Ruộng) | Mở thêm ô trồng trên map |
| Quầy bán | Tăng số đơn & giá bán tốt hơn |
| Nhà chính | Tăng giới hạn tổng thể & mở tính năng |

---

## 5. KIẾN TRÚC KỸ THUẬT
- **Nguyên tắc:** Service-based · event-driven · data-driven (ScriptableObject).
- **VContainer (DI):** quản lý vòng đời rõ ràng, testable, ít GC — hợp mobile.
- **Module nền đã có:** Input (context-aware) · Map (grid/preview) · Time (clock + sync + chống chỉnh giờ) · UI System (UICollection) · DesignPattern.
- **Map — Unity Tilemap:** Grid + nhiều layer; trồng lên tile; drag + clamp; config no-code.
- **Time System (chống cheat):** mốc = Firebase server timestamp; phát hiện tua giờ; **offline** dùng cache → **sync & validate** khi online.
- **Event System:** LifeCycle chung (Schedule → Activate → Progress → Reward → Cleanup) + config ScriptableObject.
- **UI:** UICollection (Layer/Group). **Backend:** Firebase (Auth + Cloud Save + server time).

**Tech stack:** Unity URP · Unity Tilemap · VContainer · ScriptableObject · Firebase · UICollection · Git.

---

## 6. NỘI DUNG & CÂN BẰNG
- Thời gian trồng/nuôi & độ phức tạp món **tăng dần** theo tiến trình; món cao cấp = nhiều nguyên liệu/bước hơn nhưng giá cao hơn.
- Mỗi cây/vật nuôi có sprite theo **3 giai đoạn** để đọc tiến trình bằng mắt.
- Canh nhịp các mốc unlock để luôn có mục tiêu kế tiếp ("thích cái mới").

---

## 7. PHẠM VI MVP (8 tuần)
**Trong scope:** Map (Tilemap, config, drag) · Core loop đầy đủ · Unlock + Quest · Tutorial Bánh Mì + 4–6 món + 3–5 nguyên liệu · Event "Tết: Bánh Chưng" · Cooking animation · **Update System** · Shop booster · Time chống cheat · Save offline/online (Firebase).

**Ngoài scope:** đa map/vùng miền, nhiều event (Trung Thu…), IAP/monetization, leaderboard, backend riêng, đa ngôn ngữ.

---

## 8. QUẢN TRỊ DỰ ÁN (Scrum — 4 Sprint × 2 tuần)
| Sprint | Thời gian | Goal | Backlog chính |
|---|---|---|---|
| 1 | 02–15/06 | Map + Core Loop nền | Tilemap config + drag; di chuyển/one-tap; Time System nền |
| 2 | 16–29/06 | Farm + Unlock | Trồng/nuôi theo Time + giai đoạn sinh trưởng; Unlock nguyên liệu |
| 3 | 30/06–13/07 | Cooking + Quest + Update + Shop | Cooking animation; Unlock món; Quest; Tutorial bánh mì; Update System; Shop booster |
| 4 | 14–27/07 | Event + Cutscene + Online + Polish | Event Tết; cutscene; Firebase save offline/online; UI; build demo |

**KPI:** trọn core loop không lỗi · Unlock+Quest hoạt động · ≥4 món + nguyên liệu 3 giai đoạn · Event Tết chạy + nhận thưởng · Tutorial < 3 phút · Update ≥1 công trình + buff đúng · Time chống cheat + save offline/online · **60FPS landscape ổn định**.

---

## 9. CHECKLIST HỆ THỐNG ĐỊNH LÀM

### 9.1 Game Design
- [ ] **Unlock System** — nguyên liệu + món; checklist nguyên liệu; unlock = coins *(4.3)*
- [ ] **Core Loop** — list NL → trồng/nuôi → thu hoạch → sản xuất → cutscene *(3)*
- [ ] **Quest System** — ép user làm món vừa unlock *(4.6)*
- [ ] **Event System** — Tết bánh chưng trước (quảng bá văn hoá) *(4.7)*
- [ ] **Tutorial** — dễ hiểu + món thân thuộc (bánh mì) *(4.8)*
- [ ] **Cutscene** — đồ ăn gắn nhân vật nổi tiếng *(4.10)*
- [ ] **Balance** — cân bằng thời gian + hình thái trồng/sản xuất *(6)*

### 9.2 Developer
- [ ] **Map System** — 1 map, config được, trồng lên map, drag map (Unity Tilemap) *(4.2 / 5)*
- [ ] **Farm System** — trồng/chăn nuôi theo Time System *(4.4)*
- [ ] **Cooking System** — animation cho cooking *(4.5)*
- [ ] **Time System** — chống cheat (Firebase time, sync) *(5)*
- [ ] **Shop** — booster (x2 time trồng, x2 resource…) *(4.9)*
- [ ] **Event System** — LifeCycle chung, config dễ (không đụng code) *(5)*
- [ ] **Update System** — xây & nâng cấp công trình trong farm (đổi hình theo cấp) *(4.12)*

### 9.3 Art
- [ ] **Tile cho map** — bộ tile nền Tilemap
- [ ] **Environment** — design map → thêm components trang trí
- [ ] **Nguyên liệu** — cây trồng & vật nuôi theo Progress (giống / phát triển / trưởng thành)
- [ ] **Công trình** — Bếp/Kho/Chuồng/Quầy với hình theo từng cấp (cấp 1 → N)
- [ ] **Cutscene** — thành phẩm → câu chuyện
- [ ] **UI/UX** — bộ UI màn hình ngang, icon món/nguyên liệu

---

## 10. HƯỚNG MỞ RỘNG (POST-MVP)
Đa món & vùng miền Bắc–Trung–Nam · đa event (Trung Thu…) + mini-game nấu ăn · kinh tế động (cung–cầu, thời tiết/mùa vụ) · monetization + leaderboard + backend riêng · bộ sưu tập cutscene "Câu chuyện món Việt".

---
*Cập nhật 18/06/2026 — Vũ Đình Khoa (PO / Lead Dev / Game Design).*
