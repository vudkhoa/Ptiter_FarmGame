# KỊCH BẢN CUTSCENE — "Ổ BÁNH MÌ ĐI RA THẾ GIỚI"

> **Mã hiệu:** `CUTSCENE_BANHMI_INTRO`
> **Thời lượng:** 32 giây · 6 shot
> **Định dạng:** 2.5D cutout — ảnh minh họa tĩnh, tách layer, animate bằng tween
> **Vị trí phát:** sau khi loading hoàn tất, trước khi vào scene chính
> **Ngày viết:** 26/07/2026

---

## I. LOGLINE

> *Một ổ bánh mì không bắt đầu ở lò nướng. Nó bắt đầu ở bùn đất, ở giọt mồ hôi, ở tiếng lợn ủn ỉn sau vườn, ở nắm rau còn ướt nước giếng. Ba mươi hai giây để kể lại con đường đó — từ luống lúa Việt Nam đến bảng vàng ẩm thực thế giới.*

---

## II. Ý ĐỒ ĐẠO DIỄN

Cutscene này **không** phải quảng cáo đồ ăn. Nó là một lời cảm ơn.

Năm shot đầu là **năm bàn tay** — bàn tay người gặt, bàn tay người nuôi, bàn tay người thợ, bàn tay người hái rau, và bàn tay vô hình đã gói tất cả lại thành một ổ bánh. Shot thứ sáu là lúc **thế giới nhìn thấy** những bàn tay đó.

Nhịp phim đi theo đường cong: **chậm → ấm → nóng → tươi → bùng nổ → lặng đi trong tự hào.**

Ba nguyên tắc giữ suốt kịch bản:

1. **Không có mặt người nào nhìn thẳng vào camera.** Người nông dân cúi xuống, bác nuôi lợn nhìn con lợn, người thợ quay lưng. Họ đang *làm việc*, không đang *diễn*. Đó là chỗ cái đẹp nằm.
2. **Hình ảnh kể chuyện thay lời thoại.** Không voice-over.
3. **Mỗi shot có đúng một chuyển động chính.** Thêm nữa là loãng. Ảnh tĩnh mà biết thở thì hơn ảnh động mà rối.

---

## III. RÀNG BUỘC KỸ THUẬT

Đây là những thứ **bắt buộc** vì lý do kỹ thuật. Ngoài bảng này, art toàn quyền quyết định.

| Hạng mục | Ràng buộc | Vì sao |
|---|---|---|
| Tỉ lệ khung | 16:9 | |
| **Safe margin** | Vẽ **dư 12%** mỗi cạnh | Camera có zoom/pan — thiếu margin là lộ mép ảnh. Lỗi tốn kém nhất, dễ tránh nhất |
| Độ phân giải | 2560×1440 cho layer nền | Đủ cho zoom nhẹ trên màn 1080p |
| **Tách layer** | Mọi thứ *có chuyển động riêng* phải là **layer rời** | Xem mục VII |
| Layer phát sáng | Lò nướng, hào quang, glow chữ để **file riêng** | Để chồng sáng và cho nhấp nháy độc lập |
| Chữ trong Shot 6 | **Không vẽ chữ vào ảnh** — dựng bằng font | Để sửa và dịch được nhiều ngôn ngữ |

**Phong cách vẽ, bảng màu, bố cục, ánh sáng, số lượng layer — art quyết định.** Kịch bản chỉ chốt *cảm xúc* của từng shot (mục IV) và *chuyển động chính* (mục V).

**Vì sao chọn 2.5D cutout thay vì video:** một file video 32 giây 1080p tốn 15–40 MB và không co giãn theo độ phân giải máy. Ảnh tách layer tốn khoảng 8–12 MB, sắc nét trên mọi màn hình, và quan trọng nhất — **sửa được**. Muốn đổi màu áo bác nông dân lúc 2 giờ sáng thì đổi một file PNG, không phải render lại cả đoạn phim.

---

## IV. BẢNG NHỊP TỔNG

| # | Tên shot | Vào | Ra | Dài | Cảm xúc cần đạt |
|---|---|---|---|---|---|
| 1 | **ĐẤT** — Gặt lúa | 0.0 | 5.0 | 5.0s | Tĩnh lặng, biết ơn |
| 2 | **NGƯỜI** — Nuôi lợn | 5.0 | 9.5 | 4.5s | Ấm áp, hiền hậu |
| 3 | **LỬA** — Lò bánh | 9.5 | 14.5 | 5.0s | Trang nghiêm, nhịp tim |
| 4 | **HƯƠNG** — Rau thơm | 14.5 | 18.5 | 4.0s | Tươi mát, reo vui |
| 5 | **THÀNH PHẨM** — Bánh mì heo quay | 18.5 | 24.0 | 5.5s | Kiêu hãnh, thèm thuồng |
| 6 | **VINH DANH** — Bảng xếp hạng | 24.0 | 32.0 | 8.0s | Lặng đi, tự hào |

---

## V. SÁU SHOT

> Phần này chốt **nội dung** và **chuyển động chính**. Bố cục, màu sắc, ánh sáng, cách chia layer, các chuyển động phụ — art và animator tự do sáng tạo.

---

### ▌SHOT 1 — ĐẤT · `00.0 → 05.0`

**Diễn tả:** Người phụ nữ gặt lúa giữa cánh đồng buổi chiều. Khắc họa nét đẹp của người nông dân — cái lưng cong xuống ấy là cả một đời người. Không thấy rõ mặt, chỉ thấy dáng.

Người xem chưa được biết đây là phim về bánh mì. Đó là dụng ý.

**Animation:**
- **Nhịp chính `0.6 – 2.4`** — Cúi xuống gặt lúa. Chuyển động phải *nặng*: chậm vào, chậm ra, có một khoảnh khắc gần như đứng lại ở đáy động tác. Sức nặng ấy là toàn bộ cảm xúc của shot.

**Âm thanh:** gió lùa lá lúa · tiếng liềm *sựt* khô gọn · chim gọi bầy xa xa.

---

### ▌SHOT 2 — NGƯỜI · `05.0 → 09.5`

**Diễn tả:** Bác nông dân cho lợn ăn. Bác không nhìn ta — bác nhìn con lợn của bác, và bác cười hiền. Nụ cười ấy mới là nhân vật chính của khung hình.

Shot 1 là cái đẹp của lao động. Shot 2 là cái đẹp của **sự tử tế**.

**Animation:**
- **Nhịp chính `5.5 – 9.5`** — Ba chuyển động nhỏ chạy **song song**, cùng vẽ nên một người đàn ông hiền:
  - **Nụ cười** nở rất chậm rồi giữ nguyên tới hết shot. Vai hạ xuống một nhịp thở ra: cái thở của người vừa làm xong việc mình thương.
  - **Mồ hôi** lấm tấm trên trán, và có **đúng một giọt** lăn xuống gò má. *Một giọt thôi — hai giọt là kịch, một giọt là đời.*
  - **Chú lợn ăn ngon lành** — đầu nhấp lên xuống theo nhịp nhai, tai vẫy, đuôi ngoáy tít.

**Âm thanh:** lợn ủn ỉn *ục... ục... ụt* chồng lên tiếng nhai *chóp chép* ướt · gà gáy trưa xa xa.

---

### ▌SHOT 3 — LỬA · `09.5 → 14.5`

**Diễn tả:** Người thợ làm bánh, **nhìn từ sau lưng**. Ta đứng sau lưng người đang giữ lửa. Không thấy mặt, chỉ thấy đôi vai và ánh đỏ hắt lên viền vai ấy.

Shot trang nghiêm nhất phim. Cái lò **thở** — đó là toàn bộ animation của shot này.

**Animation:**
- **Nhịp chính `9.5 – 14.5`** — Lò nướng đỏ ửng sáng lên theo nhịp hô hấp, chu kỳ **1.4 giây**: phồng lên nhanh, lịm xuống chậm. Không phải nhấp nháy đều.

  ⚠️ **Ba thứ phải nhịp CÙNG LÚC với lò**, thiếu là mất hết hiệu quả:
  - Viền sáng trên vai người thợ — đậm nhạt theo lò
  - Bóng người thợ đổ về phía camera — dài ngắn theo lò
  - Sắc đỏ ám lên toàn khung — nồng nhạt theo lò

  *Cả căn phòng phải thở theo cái lò.* Đó là điều phân biệt shot này với một ảnh tĩnh có đèn nhấp nháy.

**Âm thanh:** lửa gầm trầm liên tục · vỏ bánh nứt tanh tách.

---

### ▌SHOT 4 — HƯƠNG · `14.5 → 18.5`

**Diễn tả:** Cận cảnh rau ngò, rau thơm, dưa leo thái lát và ớt — tươi rói, còn ướt nước, lắc lắc lấp lánh. Không có người.

Sau ba shot nặng (đất, mồ hôi, lửa), khung hình này phải làm người xem **nhẹ bẫng**. Shot 3 là nhịp tim thì Shot 4 là hơi thở ra.

**Animation:**
- **Nhịp chính** — Rau lắc lư và lấp lánh. Animator tự chọn biên độ và nhịp.

  ⚠️ Chỉ một điều bắt buộc: **các cụm rau phải rung lệch pha nhau.** Rung cùng lúc thì trông như một tấm bìa rung; lệch nhau thì trông như rau thật.

**Âm thanh:** nước vẩy *tí tách* · lá cọ *xào xạc* khô nhẹ · một tiếng chuông rất nhỏ mỗi lần lấp lánh.

---

### ▌SHOT 5 — THÀNH PHẨM · `18.5 → 24.0`

**Diễn tả:** Ổ bánh mì heo quay thành phẩm, đã cắt dọc banh ra thấy hết ruột, **hào quang phía sau**. Nền tối để hào quang nổi bật.

Mục tiêu duy nhất: làm người xem **thèm**. Nhân bánh gồm những gì, xếp ra sao — art tự quyết.

**Animation:**
- **Nhịp chính `18.5 – 24.0`** — Hào quang phía sau ổ bánh sáng lên theo nhịp thở, chậm và uy nghiêm hơn nhịp lò ở Shot 3.

**Âm thanh:** một tiếng **cắn giòn *rắc!***

---

### ▌SHOT 6 — VINH DANH · `24.0 → 32.0`

**Diễn tả:** Một tờ giấy — bảng xếp hạng món ăn ngon nhất thế giới. **Cả trang đều mờ, chỉ một dòng là rõ: BÁNH MÌ — VIỆT NAM.** Chữ đen có glow trắng phía sau và bóng đổ nhẹ, hào quang phía sau tờ giấy.

Ẩn dụ trực tiếp: giữa hàng trăm món ăn của cả thế giới, có một cái tên Việt Nam hiện lên sắc nét. Người xem không cần đọc hết — mắt họ sẽ tự bị kéo về dòng duy nhất rõ nét.

Shot dài nhất và **lặng nhất** phim.

**Bố cục chữ trên giấy:**

```
   ┌────────────────────────────────────┐
   │  100 MÓN BÁNH KẸP NGON NHẤT        │  ← tiêu đề, rõ vừa
   │          THẾ GIỚI                  │
   │  ────────────────────────────      │
   │  ▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓▓▓▓▓           │  ← mờ
   │  ▓▓▓▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓              │  ← mờ
   │  ▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓            │  ← mờ
   │                                    │
   │  01.  BÁNH MÌ  —  VIỆT NAM         │  ← RÕ NÉT + GLOW
   │                                    │
   │  ▓▓▓▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓▓           │  ← mờ
   │  ▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓▓                │  ← mờ
   │  ▓▓▓▓▓▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓           │  ← mờ
   └────────────────────────────────────┘
```

Ba dòng trên và ba dòng dưới phải mờ tới mức **không đọc được chữ nào** — nhưng vẫn nhận ra *đó là chữ*, có nhịp chữ, có khoảng cách từ.

**Animation:**
- **Nhịp chính `26.0 – 27.2`** — Dòng bánh mì chuyển từ **mờ sang rõ**. Sáu dòng còn lại vẫn mờ nguyên.
- Sau đó tới hết shot — glow trắng sau dòng chữ **nhấp nháy nhẹ**. Chỉ vậy thôi, không cần cầu kỳ.

**Âm thanh:** giấy *sột soạt* · một tiếng **ngân trong** lúc dòng chữ lấy nét.

---

## VI. NỘI DUNG TỜ GIẤY (SHOT 6)

### Danh hiệu dùng trong phim

Bánh mì Việt Nam **thật sự** từng được xếp **hạng 1** trong danh sách *100 món bánh kẹp ngon nhất thế giới* (2024), và nhiều năm liền nằm trong top các bảng xếp hạng đồ ăn đường phố và bánh kẹp quốc tế. Từ *"bánh mì"* cũng đã được ghi nhận chính thức vào từ điển tiếng Anh Oxford ngày **24/03/2011**.

→ Phim dùng **hạng 1**. Có thật, kiểm chứng được, không phải bịa. Và "hạng 1" đọc trong một phần tư giây là hiểu.

### ⚠️ KHÔNG đưa tên thương hiệu lên màn hình

Tờ giấy **chỉ có** tiêu đề `100 MÓN BÁNH KẸP NGON NHẤT THẾ GIỚI`, các dòng mờ, và dòng bánh mì rõ nét.

- ❌ Không logo tòa soạn
- ❌ Không tên trang xếp hạng
- ❌ Không tên báo, không nguồn trích dẫn

Vừa sạch về pháp lý, vừa đẹp hơn về thị giác — trang giấy không bị rối.

### Sáu dòng mờ nên viết gì

Dùng tên các món bánh kẹp có thật của thế giới, xếp quanh bánh mì:

> Tombik döner *(Thổ Nhĩ Kỳ)* · Shawarma *(Trung Đông)* · Torta *(Mexico)*
> **→ BÁNH MÌ *(Việt Nam)*** ←
> Lobster roll *(Mỹ)* · Sandwich de lomo *(Argentina)* · Montreal smoked meat *(Canada)*

Đây là tên **món ăn**, không phải thương hiệu — dùng thoải mái.

Chi tiết này không ai đọc được. Nhưng nếu có người tạm dừng màn hình để soi, họ phải thấy một trang giấy thật. Đó là thứ phân biệt sản phẩm làm cẩn thận với sản phẩm làm cho xong.

---

## VII. YÊU CẦU TÁCH LAYER

Art tự quyết số lượng layer. Bảng này chỉ liệt kê những thứ **bắt buộc phải rời nhau** vì chúng chuyển động độc lập.

| Shot | Bắt buộc tách rời |
|---|---|
| 1 — ĐẤT | Người phụ nữ cần **2 tư thế** (đứng / cúi) để nội suy, hoặc rig 3 khớp hông–vai–tay |
| 2 — NGƯỜI | Bác trai: **3 khung miệng** (thẳng / hé / cười). Lợn: đầu, thân, tai, đuôi rời nhau |
| 3 — LỬA | Khối lửa = **file phát sáng riêng**. Viền sáng người thợ tách khỏi silhouette. Bóng đổ riêng |
| 4 — HƯƠNG | **Từng cụm rau tách rời hoàn toàn**, mỗi cụm có điểm gốc xoay ở cuống |
| 5 — THÀNH PHẨM | **Hào quang = file riêng**, tách khỏi ổ bánh |
| 6 — VINH DANH | Khối chữ mờ / dòng bánh mì / glow trắng / hào quang giấy — **bốn thứ riêng biệt**. Chữ dựng bằng font, không vẽ |

Nhắc lại điều quan trọng nhất: **mọi layer vẽ dư ra ngoài khung ít nhất 12%.**

---

## VIII. NHỮNG ĐIỀU CUTSCENE NÀY *KHÔNG* LÀM

Ghi rõ để tránh phình việc:

- ❌ Không lời thoại nhân vật, không voice-over
- ❌ Không mặt người nhìn thẳng camera
- ❌ Không dùng file video — toàn bộ dựng bằng ảnh tách layer
- ❌ Không tương tác — người chơi chỉ có một lựa chọn: bỏ qua
- ❌ Không phân nhánh, không thay đổi theo tiến trình game
- ❌ **Không đưa tên hay logo thương hiệu lên màn hình**

**Nút Bỏ qua:** góc dưới phải, mờ 40%, hiện sau giây 1.5.

---

## IX. CHECKLIST NGHIỆM THU

Khi cutscene chạy được, ngồi xem và tự hỏi sáu câu:

- [ ] Xem tới Shot 2 mà **vẫn chưa đoán được** phim nói về bánh mì → đúng ý đồ
- [ ] Shot 3: có cảm giác **cả căn phòng đang thở** chứ không phải chỉ cái đèn nhấp nháy?
- [ ] Shot 4: các cụm rau có rung **lệch nhau** không? Rung cùng lúc là hỏng
- [ ] Shot 5: có thấy **thèm** không?
- [ ] Shot 6: mắt có **tự động** bị kéo về dòng bánh mì, hay phải cố tìm?
- [ ] Xem hết: có thấy 5 shot đầu đều **dẫn về** ổ bánh ở Shot 5 không?

Sáu câu đều "có" — cutscene xong.

---

*"Từ bùn đất tới bảng vàng. Ba mươi hai giây, và một ngàn năm."*
