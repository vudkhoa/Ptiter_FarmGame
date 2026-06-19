# KẾ HOẠCH TRIỂN KHAI — DỰ ÁN GAME (MVP 8 TUẦN)

---

## 0. NHÂN SỰ & SỨC LÀM (capacity)

| Người | Vai trò chính | Domain phụ trách |
|---|---|---|
| **Vũ Đình Khoa** | PO · Lead Dev · GD | Kiến trúc/DI, **Map**, Time (chống cheat), Firebase Save, Event core, code review, build |
| **Phạm Hùng Thiên** | Dev · GD | Design hoàn chỉnh (feature + flow), **cân bằng số liệu**, Update System (công trình), Shop/Booster, tích hợp UI |
| **Văn Minh Tấn** | Dev | Farm, Cooking, Unlock + Quest, Tutorial |
| **02 SV Đa phương tiện** | Art | Tile/Environment & UI/UX · Nguyên liệu & Công trình & asset Cutscene |

---

## 1. PHÂN CHIA ĐẦU VIỆC

Chia theo **domain** để mỗi người làm chủ 1 mảng (ít đụng code nhau, ghép qua **event + DI**).
Bảng dưới gồm **toàn bộ việc trong 8 tuần** (Tuần 0 + Dev + Polish). Giai đoạn: `T0` = chuẩn bị · `Dev` = 6 tuần · `Polish` = 2 tuần cuối.

### 🅰 Khoa
| Việc | Mô tả | Giai đoạn | SP |
|---|---|---|---|
| Hạ tầng/base | Event Bus, base Service/DI, LifetimeScope, build pipeline | T0 | 5 |
| Map System | Unity Tilemap nhiều layer; `MapConfig` (SO); trồng lên ô; drag + clamp biên | Dev | 5 |
| Time System (chống cheat) | `ClockService` lấy Firebase server time; phát hiện tua giờ; offline cache → sync/validate | Dev | 5 |
| Firebase Save | Auth + Cloud Save; offline/online; resolve khi reconnect | Dev | 5 |
| Event System (core) | LifeCycle chung `Schedule→Activate→Progress→Reward→Cleanup`; đọc `EventConfig` (SO) | Dev | 4 |
| **Cutscene #1** | Ghép art + trigger sau khi hoàn thành 1 món signature | Polish | 2 |
| Save online hoàn chỉnh + Build demo | Test sync/validate khi reconnect; build Android máy target | Polish | 3 |
| Code review | Review PR, giữ chuẩn kiến trúc cho cả team (xuyên suốt) | T0→Polish | — |

### 🅱 Thiên
| Việc | Mô tả | Giai đoạn | SP |
|---|---|---|---|
| GDD + Design hoàn chỉnh | Hoàn thiện GDD; thiết kế **feature + flow** chi tiết cho từng hệ thống (spec cho cả team) | T0 | 5 |
| Cân bằng số liệu | Cân `growTime`/giá/checklist/hệ số booster trong **SO** → Khoa check & chốt | Dev | 3 |
| Update System | Xây/nâng cấp công trình (Bếp/Kho/Chuồng/Quầy); đổi hình theo cấp + buff | Dev | 4 |
| Shop & Booster | `BoosterConfig` (SO); mua bằng coins; áp multiplier vào Farm/Cooking | Dev | 3 |
| UI tích hợp | Ghép data vào **UICollection (đã setup sẵn)**: inventory, checklist nguyên liệu, shop, quest | Dev | 4 |
| **Cutscene #2** | Ghép art + trigger sau khi hoàn thành 1 món signature | Polish | 2 |
| UI/UX polish | Hoàn thiện layout ngang, icon, popup theo playtest | Polish | 2 |
| Design xuyên suốt | Tinh chỉnh feature/flow theo phản hồi playtest mỗi sprint | Dev→Polish | — |

### 🅲 Tấn
| Việc | Mô tả | Giai đoạn | SP |
|---|---|---|---|
| Farm System | `CropData`/`AnimalData` (SO); nghe `ClockService`; 3 giai đoạn sinh trưởng; thu hoạch → event | Dev | 5 |
| Cooking System | `RecipeData` (SO); hàng đợi; thời gian theo Clock; trigger Animation | Dev | 4 |
| Unlock + Quest | Unlock nguyên liệu/món bằng coins; Quest ép làm món (Tutorial/Main/Daily) | Dev | 4 |
| Tutorial (Bánh Mì) | Dẫn người mới qua core loop < 3 phút | Dev | 2 |
| **Cutscene #3** | Ghép art + trigger sau khi hoàn thành 1 món signature | Polish | 2 |

> **Cả team (Polish):** Bug fix + Profiling giữ **60FPS** + QA pass core loop.

### 🅳 Balance — Thiên làm, Khoa chốt
**Thiên (GD)** cân `growTime` / giá bán / checklist nguyên liệu / hệ số booster trong **ScriptableObject**; **Khoa check qua & chốt** để đảm bảo nhịp core loop & độ khó đồng nhất.

### 🅳 Art (2 SV) — chạy song song, giao asset theo sprint
| Mảng | Nội dung |
|---|---|
| Art 1 | Tile nền + Environment (design map, trang trí) **+** UI/UX (layout ngang, icon món/nguyên liệu) |
| Art 2 | Nguyên liệu (cây/vật nuôi 3 giai đoạn) + Công trình (hình theo cấp 1→N) **+** asset Cutscene |

> **Cutscene asset** ưu tiên làm sau (dồn vào phase Polish) vì không chặn core loop. Dev ghép cutscene ở Polish (mỗi dev 1 cái).

---

## 2. CÔNG NGHỆ SỬ DỤNG (tech stack)

| Lớp | Công nghệ | Lý do chọn |
|---|---|---|
| Engine | **Unity (URP)** | 2.5D/2D mobile, pipeline nhẹ, tối ưu 60FPS landscape |
| Map | **Unity Grid + Tilemap** | Layer (Ground/Plantable/Decor/Collision), config no-code |
| DI | **VContainer** | Vòng đời rõ, testable, ít GC — hợp mobile *(xem [Base DI](Base_DI_VContainer.md))* |
| Data | **ScriptableObject** | Data-driven: đổi món/giá/event không sửa code |
| Kiến trúc | **Service-based + Event-driven (payload)** | Decoupled, dễ mở rộng, thêm module không phá lõi |
| Backend | **Firebase** (Auth · Cloud Save · Server Time) | Có sẵn server time → chống cheat; save online/offline |
| UI | **UICollection** (Layer/Group) | Quản lý màn hình/popup nhất quán |
| Animation | **Animator** (hoặc Spine) | Cooking state idle→cooking→done |
| Quản lý mã | **Git** (branch + PR review) | Làm việc nhóm, review chất lượng |

---

## 3. QUY TRÌNH LÀM GAME ĐẠT CHUẨN

### 3.1 Quy trình theo Scrum (vòng lặp 2 tuần)
```
Sprint Planning  → chốt backlog + chia SP
   → Daily standup (15')  → cập nhật/khơi thông chặn
   → Dev + Art chạy song song (data-driven nên ghép sớm)
   → QA pass cuối sprint (test core loop + profiling 60FPS)
   → Sprint Review (demo cho mentor) → Retro (cải tiến)
```

### 3.2 Chuẩn code (Definition of Done — DoD)
Một feature coi là **xong** khi:
- [ ] Theo kiến trúc: **Service + DI + event**, *không* `FindObject`/`new` chéo, *không* `Update` đếm giờ (dùng `ClockService`).
- [ ] **Data ra ScriptableObject** (GD chỉnh được, không cần code).
- [ ] **Zero-alloc** ở hot path; tôn trọng **DRY / SRP**; không leak (unsub event khi hủy).
- [ ] Qua **PR review** của Lead; merge vào `develop` (không push thẳng `main`).
- [ ] Test tay đúng acceptance + giữ **60FPS** trên máy target.

### 3.3 Git flow
`main` (ổn định) ← `develop` ← `feature/<tên>` (mỗi người 1 nhánh/feature) → **PR → review → merge**.

### 3.4 Quy trình asset (Art ↔ Dev)
Art giao theo **naming convention + đúng size/pivot** → Dev nhét vào **SO/Prefab** → ghép vào hệ thống.
→ Art không cần biết code; Dev không chờ art mới chạy logic (dùng placeholder).

---

## 4. ƯỚC LƯỢNG THỜI GIAN (tổng ~8 tuần: 1 tuần chuẩn bị + 6 tuần dev + 2 tuần polish)

> Cấu trúc: **Tuần 0** (chuẩn bị) → **6 tuần DEV** (3 sprint × 2 tuần) → **2 tuần POLISH**.
> Lý do thứ tự: **mọi hệ thống phụ thuộc Time + Map** → Khoa làm nền trước; rủi ro cao (Firebase/chống cheat) đẩy sớm.

### Giai đoạn chuẩn bị — Tuần 0
| Khoa | Thiên
|---|--|
| Base: Event Bus, Service/DI, LifetimeScope, Git/build pipeline | Hoàn thiện **GDD** + thiết kế chi tiết cách feature hoạt động |

### Giai đoạn DEV — 6 tuần (3 sprint)
| Sprint | Tuần | Goal | Khoa | Thiên | Tấn |
|---|---|---|---|---|---|
| **1** | 1–2 | Nền: Map + Time | Map System (Tilemap + drag) → Time System (chống cheat) | Update System (công trình) | Farm (3 giai đoạn) |
| **2** | 3–4 | Core loop + Kinh tế | Firebase Save (nền) | Shop/Booster + UI inventory | Cooking + Unlock nguyên liệu/món |
| **3** | 5–6 | Hoàn chỉnh loop + Event | Event System (core) + Event Tết | UI tích hợp (checklist/shop/quest) | Quest + Tutorial (Bánh Mì) |

### Giai đoạn POLISH — 2 tuần (Tuần 7–8)
| Hạng mục | Ai | Ghi chú |
|---|---|---|
| **Cutscene (3 cái)** | **Mỗi dev 1 cái** (Khoa/Thiên/Tấn) | Ghép art + trigger sau khi hoàn thành món signature |
| UI/UX hoàn thiện | Thiên dẫn | Polish layout ngang, icon, popup |
| Save online/offline hoàn chỉnh | Khoa | Test sync/validate khi reconnect |
| Balance số liệu | **Thiên** làm, **Khoa** chốt | Cân nhịp core loop, độ khó |
| Bug fix + Profiling 60FPS | Cả team | QA pass, tối ưu hot path |
| Build demo | Khoa | Build Android, kiểm thử trên máy target |

### Mốc kiểm tra (milestone)
- **Hết Tuần 0:** base DI chạy + GDD/spec chốt → cả team có nền & spec để code.
- **Hết Sprint 1:** đi lại/kéo map, đồng hồ theo server time, trồng cây khung. ✅ nền vững.
- **Hết Sprint 2:** trồng→thu hoạch + nấu + shop + unlock. ✅ gần trọn core loop.
- **Hết Sprint 3:** **trọn core loop** + Quest + Tutorial + Event Tết chạy. ✅ chơi được (feature-complete).
- **Hết Polish:** 3 cutscene + save online/offline + 60FPS → **demo MVP**. 🎯

### Rủi ro & đệm thời gian
| Rủi ro | Ảnh hưởng | Cách giảm |
|---|---|---|
| Firebase/chống cheat phức tạp | Trễ nền online | Offline-first; online ghép & validate sau |
| Art trễ | Dev chờ | Placeholder + naming convention; cutscene dồn vào polish |
| Scope phình | Trễ MVP | Bám **đúng scope GDD**; tính năng ngoài → post-MVP |

> **Kết luận:** Feature-complete trong **6 tuần dev**, **2 tuần polish** (gồm 3 cutscene + build) → tổng ~8 tuần, cộng **Tuần 0** chuẩn bị. Khả thi *nếu* giữ scope, làm nền Time/Map trước, và GDD chốt sớm ở Tuần 0.