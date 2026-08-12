# Tutorial Flow — Kiến trúc (tóm tắt)

> Mục tiêu: hiểu nhanh cách bàn tay hướng dẫn chạy, config ở đâu, tiến độ lưu thế nào.
> Code: `Assets/Module/Tutorial/`.

## 1. Ý tưởng chính

- **`TutorialService` (POCO, ROOT scope)** giữ "step nào đang hiện". Nó chỉ biết `TutorialSignal` — không biết Farm/Map là gì.
- **`TutorialGameplaySignalBridge` (GAME scope)** là chỗ DUY NHẤT biết cả payload Farm/Map lẫn `TutorialSignal`. Đổi gameplay chỉ sửa file này.
- **`TutorialUIContainer`** nằm sẵn trong scene Preloading, làm con của `RootLifetimeScope` → ăn theo `DontDestroyOnLoad` của nó, KHÔNG tạo thêm root persistent thứ hai. Sống xuyên mọi lần load scene.
- **Config toàn bộ bằng SO**: catalog → flow → step. Mỗi step tự khai anim / vị trí / rotate của bàn tay.
- **Tiến độ lưu theo từng step id** trong `PlayerData.Tutorial`, ghi đĩa ngay (`SaveImmediate`) mỗi khi xong 1 step.

```
Tutorial  (tầng thấp — KHÔNG ref Farm/Map)
  ITutorialService · ITutorialRepository · ITutorialView
  TutorialService · TutorialAnchorRegistry · TutorialHandView · TutorialUIContainer
  TutorialCatalogSO · TutorialFlowSO · TutorialStepSO · TutorialHandConfig
  TutorialSignal · TutorialHandAnimation · TutorialAnchorMode

Tutorial.FarmIntegration ──▶ Tutorial, Farm, Map
  TutorialGameplaySignalBridge   (payload Farm/Map → TutorialSignal)

MyOwn (app) ──▶ Tutorial + Tutorial.FarmIntegration
  PlayerDataHolder : ITutorialRepository
```

Assembly tách đôi để module Tutorial không phải reference Farm/Map — y hệt cách `Storage.FarmIntegration` đang làm.

## 2. Cấu trúc config

```
TutorialCatalogSO                       Assets/Module/Tutorial/Configs/TutorialCatalog.asset
 ├ tutorialEnabled        bool          tắt toàn bộ tutorial mà không đụng save
 ├ skipForExistingSaves   bool          save cũ (đã chơi trước khi có tutorial) → đánh dấu xong hết
 └ flows                  List<Flow>    CHẠY ĐÚNG THỨ TỰ LIST

TutorialFlowSO
 ├ flowId                 string        id lưu trong save
 ├ startSignal            TutorialSignal   None = chạy ngay; khác None = nằm chờ signal đó
 └ steps                  List<Step>    chạy từ trên xuống, step đã xong thì bỏ qua

TutorialStepSO
 ├ stepId                 string        id lưu trong save (đổi tên = replay lại step đó)
 ├ hand                   TutorialHandConfig
 │   ├ anchorMode         Anchor | ScreenPercent | WorldPoint
 │   ├ anchorId           string        khớp với TutorialAnchor hoặc anchor do bridge ghim
 │   ├ screenPercent      Vector2       dùng khi anchorMode = ScreenPercent
 │   ├ worldPosition      Vector3       dùng khi anchorMode = WorldPoint
 │   ├ offset             Vector2       lệch khỏi anchor (canvas unit)
 │   ├ rotation           float         xoay Z (độ)
 │   ├ scale / flipX
 │   ├ animation          None|Tap|Press|Swipe|Pulse
 │   └ animationDuration / animationLoopDelay / animationStrength / swipeOffset
 ├ hintText               string        text tiếng Việt, rỗng = ẩn bong bóng
 ├ hintOffset             Vector2       đo từ ĐÁY BÀN TAY, không phải từ anchor
 ├ showFocusRing / focusPadding / focusFallbackSize
 ├ blockInputOutsideFocus bool          che input ngoài vùng focus
 ├ startDelay             float         chờ animation window mở xong rồi mới hiện tay
 └ completionSignal       TutorialSignal   signal làm step này xong
```

## 3. Bảng signal

`TutorialSignal` là hợp đồng giữa gameplay và tutorial. Bridge dịch từ payload sang:

| Signal | Nguồn (payload) | Điều kiện lọc |
|---|---|---|
| `LandPlacementStarted` | `MapPlacementStartedPayload` | `ObjectId` có `Kind == Soil` |
| `LandPlaced` | `MapFurnitureAddedPayload` | `AnimatePlacement == true` **và** `Kind == Soil` |
| `SeedSelectorOpened` | `OpenFarmSelectorUIPayload` | `!IsAnimal` |
| `SeedPlanted` | `FarmEntityPlantedPayload` | `EntityType == Crop` |
| `CropRipe` | `FarmEntityRipePayload` | `EntityType == Crop` |
| `CropHarvested` | `FarmEntityHarvestedPayload` | `EntityType == Crop` |

> **`AnimatePlacement` là bắt buộc**: `MapFurnitureAddedPayload` được bắn lại cho MỌI ô đất đã lưu mỗi lần load scene (`RestoreSavedPlacements`). Chỉ lần đặt do người chơi bấm mới có animate — thiếu filter này thì step "đặt đất" tự xong ngay khi vào game.

Bridge cũng **ghim world anchor** cho mục tiêu chưa có component để gắn `TutorialAnchor`:

| Anchor id | Ghim khi | Xoá khi |
|---|---|---|
| `tutorial.soil_button` | `TutorialAnchor` trên nút Soil trong `ObjectSelectScreen.prefab` (OnEnable) | OnDisable |
| `tutorial.free_plot` | `LandPlacementStarted` → ô trống **đặt được** gần tâm màn hình | `LandPlaced`, hoặc bridge Dispose |
| `tutorial.new_plot` | `LandPlaced` → `SnappedWorld + (0, 0.6, 0)`; **và lúc load scene** → ô đất trống gần tâm màn hình nhất trong save | `SeedPlanted`, hoặc bridge Dispose |
| `tutorial.ripe_crop` | `CropRipe` → `CellToWorld(cell) + (0, 0.6, 0)` | `CropHarvested`, hoặc bridge Dispose |

`tutorial.free_plot` quét vòng tròn lan ra từ chỗ tâm màn hình cắt mặt đất, hỏi `IMapService.CanPlaceObjectAt(soilId, cell)` cho từng ô, lấy ô hợp lệ gần nhất. Anchor được ghim **trước** khi `ReportSignal` — chính signal đó hoàn thành step trước và mở step này ngay trong cùng lời gọi, ghim sau thì frame đầu không có gì để chỉ.

Quét chạy **hai lượt**: lượt đầu bắt ô phải nằm trong `SafePlotViewport` (viewport `0.38–0.80` ngang, `0.30–0.72` dọc), lượt sau bỏ ràng buộc đó. Vùng an toàn tránh panel Objects bên trái, HUD trên đỉnh và nút Cancel dưới đáy — vì ô đặt ở đây còn được **step kế tiếp** chỉ vào, lúc đó panel đã hiện lại và sẽ che mất ô nằm nép sau nó. Đổi vùng thì sửa `SafePlotViewport` trong `TutorialGameplaySignalBridge`.

`CanPlaceObjectAt` là API mới thêm vào `IMapService`, chạy đúng bộ kiểm tra mà `AddFurniture` dùng nhưng không commit gì. Không có nó thì tutorial phải chép lại luật validate của Map và sẽ mục dần.

Offset `(0, 0.6, 0)` khớp `FarmVisualizer._offset` để tay chỉ đúng vào cây, không phải mặt đất dưới cây.

> **Anchor ghim runtime chỉ sống trong session ghim nó.** Bước nào cần hiện lại sau khi restart thì bridge phải **ghim lại lúc vào scene** — `tutorial.ripe_crop` quét `ActiveSlots` trong `Start`, `tutorial.new_plot` bắt các payload restore (xem ngay dưới). Cách còn lại là `WorldPoint` với một điểm cố định thật sự trên map; đừng dùng nó cho mục tiêu do người chơi đặt, vì toạ độ cứng sẽ chỉ vào chỗ khác chứ không phải đất của họ.

**`tutorial.new_plot` ghim lại qua chính payload restore.** `RestoreSavedPlacements` bắn `MapFurnitureAddedPayload` với `AnimatePlacement == false` cho mọi ô đã lưu; bridge lấy các ô `Soil` **chưa có cây** trong đó, chọn ô gần tâm màn hình nhất. Không quét map trong `Start` vì `MapService` là MonoBehaviour và restore nằm trong `Start` của nó — bridge được dựng lúc `Awake` của scope, quét sớm hơn thì grid còn rỗng. Đi theo payload thì thứ tự `Awake → Start` tự bảo đảm bridge đã subscribe trước khi restore chạy.

## 4. Hai flow hiện có

```
Flow "first_farm"  (startSignal = None → chạy ngay từ lần đầu vào game)
 ├ 1. farm_open_land_ui   tay chỉ nút Soil trong ObjectSelectScreen
 │      anchor = tutorial.soil_button · anim Tap · CHE input ngoài nút
 │      xong khi ← LandPlacementStarted
 ├ 2. farm_place_land     tay ở giữa màn hình, chỉ vào khu đất trống
 │      anchor = ScreenPercent(0.5, 0.46) · anim Press · KHÔNG che input (cần rê tự do)
 │      xong khi ← LandPlaced
 └ 3. farm_plant_seed     tay chỉ ĐÚNG ô đất người chơi vừa đặt
        anchor = tutorial.new_plot · anim Tap
        KHÔNG che input (seed picker đè lên)
        xong khi ← SeedPlanted

Flow "first_harvest"  (startSignal = CropRipe → nằm ngủ tới khi có cây chín)
 └ 1. harvest_first_crop  tay chỉ cây chín
        anchor = tutorial.ripe_crop · anim Tap · KHÔNG che input
        xong khi ← CropHarvested
```

## 5. Trình tự runtime

```
Scene Preloading
 └─ RootLifetimeScope.Awake → base.Awake() build container
      └─ RegisterTutorialModule(options, _tutorialCatalog, _tutorialUIContainer)
           • broker: StepStarted / StepCompleted / FlowCompleted
           • RegisterInstance(catalog)        ← thiếu catalog thì tạo catalog rỗng disabled, KHÔNG throw
           • RegisterInstance(container)      ← thiếu container thì bind NullTutorialView
           • RegisterEntryPoint<TutorialService>()
    (container là con của RootLifetimeScope → tự sống qua DontDestroyOnLoad của root)

 └─ TutorialService.StartAsync
      ├─ await _repository.WaitUntilLoadedAsync(ct)   ← PlayerDataHolder cũng là IAsyncStartable,
      │                                                  chờ nó chứ không đua với nó
      ├─ Initialize: đọc PlayerData.Tutorial
      │    • lần đầu chạm save (initialized == false):
      │        skipForExistingSaves && !IsNewPlayer → đánh dấu XONG HẾT mọi flow (người chơi cũ)
      └─ Advance()

Scene MapScene
 └─ GameLifetimeScope.Configure → RegisterTutorialFarmIntegration()
 └─ TutorialGameplaySignalBridge.Start
      └─ quét ActiveSlots: có crop nào state == Ripe thì ghim anchor + bắn lại CropRipe
         (FarmEntityRipePayload chỉ bắn ĐÚNG LÚC chín — thoát game trước khi thu hoạch
          thì vào lại không có event nào, flow 2 sẽ không bao giờ mở)
 └─ MapService.Start → RestoreSavedPlacements
      └─ bridge nhận payload không-animate: ghim lại tutorial.new_plot vào ô đất
         trống gần tâm màn hình nhất (cho người chơi đặt đất xong rồi thoát game)

Vòng lặp chính
 ReportSignal(signal)
   ├─ đang có step? → signal khớp completionSignal → CompleteStep → SaveImmediate → Advance
   └─ chưa có step, flow đang chờ? → signal khớp startSignal → mở khoá flow → Advance

 Advance()  (vòng lặp, không đệ quy)
   ├─ flow = flow đầu tiên trong catalog chưa xong
   ├─ flow == null                → Finish, ẩn tay
   ├─ flow chưa được mở khoá      → ẩn tay, nằm chờ startSignal
   ├─ step = step đầu tiên chưa xong trong flow
   ├─ step == null                → CompleteFlow → lặp tiếp sang flow sau
   └─ ShowStep(step)              → view.ShowStep
```

## 6. View — bàn tay bám mục tiêu

`TutorialHandView.LateUpdate` **resolve lại anchor mỗi frame**, không phải chỉ 1 lần lúc show:

```
LateUpdate
 ├─ _elapsed += unscaledDeltaTime         (unscaled: popup pause timeScale thì tay vẫn nhảy)
 ├─ chưa qua startDelay → return
 ├─ TryResolveAnchor
 │    • ScreenPercent → screenPoint = percent × Screen size
 │    • WorldPoint    → worldCamera.WorldToScreenPoint(hand.worldPosition)
 │    • Anchor + RectTransform → GetWorldCorners → screen rect (anchor UI)
 │    • Anchor + Transform/world point → worldCamera.WorldToScreenPoint
 │      (z <= 0 → bỏ: điểm sau lưng camera bị WorldToScreenPoint lật ngược chứ không báo lỗi)
 │    └─ ScreenPointToLocalPointInRectangle → toạ độ canvas
 ├─ KHÔNG resolve được → ẩn hết visual, chờ tiếp
 │    (scene chưa load xong / ô đất chưa spawn — thà không hiện còn hơn chỉ vào chỗ trống)
 └─ resolve được → đặt _handRoot, vẽ focus ring, xếp 4 blocker, đặt bong bóng hint
```

Vì vậy tay bám theo camera pan, và bám được cả ô đất spawn sau khi step đã bắt đầu.

**Pivot nằm ở đầu ngón trỏ.** RectTransform `Hand` có `m_Pivot = (0.087, 0.98)` — đúng vị trí đầu ngón trong sprite, đo từ alpha của `tutorial_hand.png` (400×326). Hai hệ quả:

- `offset = (0,0)` nghĩa là **đầu ngón chạm đúng anchor**, không phải tâm ảnh. `offset` giờ chỉ còn là nhích thêm cho đẹp.
- `rotation` xoay quanh đầu ngón, và `DOScale` lúc tap cũng co giãn quanh đầu ngón → ngón trỏ **đứng yên tại mục tiêu** suốt animation, chỉ có bàn tay nhấn vào. Đây là thứ làm cú tap trông thật.

Đổi art bàn tay thì phải đo lại `HandFingertipPivot` trong `TutorialProjectSetup.cs`, và `HandSize` phải giữ đúng tỉ lệ ảnh (Image không bật `preserveAspect`).

**Che input: KHÔNG khoét lỗ, mà clone widget lên trên lớp tối.**

```
Hand View
├── Dimmer      đen alpha 0.62, phủ kín, raycastTarget = true  → nuốt mọi tap
├── Highlight   RectTransform, MẶC ĐỊNH INACTIVE
│   ├── Fake Target    bản clone của widget, đã tước hết script, KHÔNG nhận raycast
│   └── Click Catcher  Image alpha 0.001, chuyển tiếp tap về widget thật
└── Foreground  Canvas overrideSorting 201
    ├── Focus Ring · Hand Root · Hint Root
```

Lỗ chữ nhật không bao giờ khớp nút bo góc, icon lồng nhau hay widget hình bất kỳ — nhìn ra ngay là lỗ vuông. Bản clone thì khớp silhouette theo định nghĩa, không cần shader stencil.

Bốn chi tiết bắt buộc, sai một cái là hỏng:

1. **`Highlight` phải inactive lúc `Instantiate`.** Clone vào parent inactive thì `Awake`/`OnEnable` của bản sao không bao giờ chạy. Không có nó thì `TutorialAnchor` trên clone sẽ tự đăng ký cùng `anchorId`, mà registry lấy bản đăng ký mới nhất → tay quay sang chỉ vào chính cái clone.
2. **`SanitizeClone` tước mọi `MonoBehaviour`** trừ `Graphic` và component layout. Giữ lại `Button`/`MapPlacer` là clone tự bắt sự kiện, bấm một phát chạy hai lần.
3. **`Click Catcher` chuyển tiếp tap** bằng `ExecuteEvents.ExecuteHierarchy` — dùng `Hierarchy` vì anchor có thể nằm trên con của object mang `Button`.
4. **Clone giữ nguyên `rect.size` của bản gốc**, còn `Highlight` co giãn `localScale` cho khớp kích thước đo được trên màn hình. Ép size thẳng vào clone sẽ làm con cháu bên trong lệch layout.

**Phân tầng canvas:**

| Canvas | sortingOrder |
|---|---|
| Quest HUD launcher | -100 |
| `Test_UIManager` (mọi window của UIManager) | 0 |
| Settings HUD launcher | 100 |
| `[Tutorial UI Container]` — dim + clone | **199** |
| `Foreground` — tay, ring, hint | **201** |
| Modal đang giữ input (`GlobalModalInputBlocker`) | max + 10 |

Trong container, độ sâu do **thứ tự sibling** quyết định chứ không phải sortingOrder. Chỉ container 199 là root canvas nên modal thật (Inventory / Quest / Cutscene) vẫn đè lên tutorial, đúng thiết kế.

**Hai cờ tách bạch:**

| Cờ | Việc |
|---|---|
| `dimBackground` | có làm tối nền hay không |
| `blockInputOutsideFocus` | lớp tối có nuốt tap hay không — **chỉ hiệu lực với anchor UI** |

Anchor world (ô đất, cây) được chạm qua raycast của Map, không đi qua EventSystem. Lớp tối nuốt tap sẽ khiến step không thể hoàn thành, nên với anchor world nó **chỉ làm tối**, `raycastTarget` tự tắt bất kể cờ. Vùng cần chạm được đánh dấu bằng focus ring vẽ đè lên lớp tối.

`focusSprite` cho phép đổi ảnh ring theo step — bước đặt đất dùng `tutorial_cell_diamond.png` (hình thoi isometric) thay cho khung bo góc mặc định.

`hiddenAnchorIds` làm mờ những anchor cản đường trong lúc step chạy — bước đặt đất ẩn `tutorial.objects_panel` để panel Objects không che khu đất. Dùng `CanvasGroup` (alpha + blocksRaycasts) chứ không `SetActive`, để không báo cho UIManager rằng window đã đóng sau lưng nó. Trạng thái cũ được khôi phục khi step kết thúc.

## 6b. Nhịp animation — một sequence duy nhất

Bàn tay và widget được highlight nằm chung **một** `Sequence`, không phải hai. Hai loop riêng có chu kỳ khác nhau sẽ trôi lệch pha dần, nhìn ra thành "nút tự phình to nhỏ thất thường".

Một nhịp chia hai cửa sổ:

```
0 ─────────── press ──────────────── press + release ── loopDelay ─┐
│  tay ẤN XUỐNG  (scale → animationStrength, pressEase)            │
│  widget PHÌNH TO (scale → highlightPulseScale, pressEase)        │
│                  tay NHẤC LÊN (scale → 1, releaseEase)           │
│                  widget XẸP LẠI (scale → 1, releaseEase)         │
└──────────────────────────────────────────────────────────── lặp ─┘

press   = animationDuration × pressRatio
release = animationDuration − press
```

Tay ấn xuống thì UI phình ra, tay nhấc lên thì UI xẹp lại — hai chiều luôn ngược nhau vì dùng chung một mốc thời gian.

| Field | Ở đâu | Ý nghĩa |
|---|---|---|
| `animationDuration` | `hand` | độ dài một nhịp |
| `pressRatio` | `hand` | phần nhịp dành cho lúc ấn (0.05–0.95, mặc định 0.4) |
| `pressEase` / `releaseEase` | `hand` | easing hai cửa sổ (mặc định `InQuad` / `OutBack`) |
| `animationStrength` | `hand` | tay co lại còn bao nhiêu (0.82) |
| `animationLoopDelay` | `hand` | nghỉ giữa hai nhịp |
| `highlightPulse` / `highlightPulseScale` | step | widget phình tới đâu (1.07) |

`BuildMotion` được gọi lại mỗi khi clone sinh ra hoặc mất đi — clone là một trong các target của sequence, mất nó giữa chừng mà không rebuild thì DOTween tween vào transform đã huỷ.

> Khung hint tự **clamp trong lòng canvas** (chừa mép 24 unit). Không có nó thì mục tiêu nằm sát mép màn hình sẽ đẩy bong bóng ra ngoài và cắt mất chữ.

## 7. Lưu tiến độ

```
PlayerData.Tutorial : TutorialSaveData     (SaveVersion 4 → 5)
 ├ initialized       bool            đã soi save này lần nào chưa
 ├ completedStepIds  List<string>
 └ completedFlowIds  List<string>
```

Đi qua `ITutorialRepository` (`PlayerDataHolder` implement, giống `ICurrencyRepository` / `IProgressQuestRepository`) — module Tutorial không biết `MyOwn.ServiceHarness` là gì.

| Quy tắc | Lý do |
|---|---|
| `SaveTutorial` gọi `SaveImmediate`, không phải `Save` throttle | step tính xong trong RAM mà crash mất thì replay cả nhịp |
| `SaveImmediate` fail → rollback field trong RAM | khớp cách `ProgressQuestService` / `DailyQuestService` xử lý |
| Chỉ lưu id ĐÃ XONG, không lưu "đang ở step mấy" | thêm step vào flow cũ chỉ replay đúng step mới |
| `initialized` tách "máy mới" khỏi "save có trước tutorial" | `IsNewPlayer` chỉ đúng ở session tạo file save; lần sau nó false |
| `PlayerDataSaveLoad.NormalizeLoadedData` backfill `Tutorial` | JsonUtility để null field mà save cũ không có |

## 8. Thêm step / flow mới

1. `Create > Game > Tutorial > Tutorial Step`, đặt `stepId` (ĐỘC NHẤT, đừng đổi sau khi ship).
2. Chọn `anchorMode`:
   - `Anchor` — trỏ vào UI/object có sẵn: gắn `TutorialAnchor` lên nó, điền cùng `anchorId`;
     hoặc trỏ vào thứ sinh lúc runtime: bridge gọi `TutorialAnchorRegistry.SetWorldPoint(id, worldPos)`;
   - `WorldPoint` — mục tiêu luôn ở một chỗ cố định trên map: điền thẳng `worldPosition`, không phụ thuộc session;
   - `ScreenPercent` — không phụ thuộc scene lẫn camera.

   Cả 3 mode đều dùng chung `offset`, `rotation`, `scale`, `flipX` để tinh chỉnh vị trí và hướng bàn tay.
3. Cần signal mới thì thêm vào `enum TutorialSignal` **và** dịch nó trong `TutorialGameplaySignalBridge`. Không sub payload Farm/Map ở đâu khác.
4. Kéo step vào `TutorialFlowSO.steps`, kéo flow vào `TutorialCatalogSO.flows` đúng thứ tự muốn chạy.

> `completionSignal = None` nghĩa là step chạy-qua-luôn (hiện rồi xong ngay). `Advance` dùng vòng lặp có trần 512 để catalog sai không treo game.

## 9. Debug

| Việc | Cách |
|---|---|
| Chơi lại tutorial từ đầu | **F9** trong Editor / development build (`TutorialDebugController`) — giữ nguyên phần save còn lại |
| Dựng lại toàn bộ asset + wiring | `Tools > Tutorial > Rebuild Tutorial Content` |
| Xoá hẳn save | `Tools > Tutorial > Reset Saved Tutorial Progress` (mở thư mục chứa `playerdata.json`) |
| Tắt tutorial | bỏ tick `tutorialEnabled` trên `TutorialCatalog.asset` |
| Người chơi cũ vẫn muốn xem tutorial | bỏ tick `skipForExistingSaves`, rồi xoá save hoặc bấm F9 |

`Tools > Tutorial > Rebuild Tutorial Content` tự sinh: sprite placeholder trong `Texture/`, SO trong `Configs/`, `[Tutorial UI Container]` trong scene Preloading (kèm gán 2 field trên `RootLifetimeScope`), và `TutorialAnchor` trên nút Soil của `ObjectSelectScreen.prefab`. Nó chạy tự động nếu chưa có `TutorialCatalog.asset` — **và nó SAVE scene Preloading**, nên commit/stash thay đổi chưa lưu trước khi để Unity reload.

## 10. Việc còn lại

- `Texture/tutorial_hand.png` đang là placeholder (chấm tròn + vòng ngoài) do script sinh ra. Thay bằng art bàn tay thật, không cần đụng code.
- 4 step hiện tại không chỉ vào bên trong modal nào. Nếu sau này cần, phải xử lý `GlobalModalInputBlocker` đẩy modal lên `max + 10` (cao hơn canvas tutorial).
