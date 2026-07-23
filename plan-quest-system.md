# Kế hoạch triển khai Quest System

## 1. Mục tiêu và phạm vi

Hệ thống nhiệm vụ sẽ có ba tab:

- **Daily**: nhiệm vụ hằng ngày, tự kích hoạt, reset theo thời gian máy chủ.
- **Progress**: nhiệm vụ tiến trình, có chuỗi nhiệm vụ và mốc thưởng; triển khai sau Daily.
- **Food**: bộ sưu tập món ăn được mở khóa từ Progress; triển khai sau Daily.

Giai đoạn hiện tại chỉ triển khai hoàn chỉnh **Daily**, nhưng Quest Core phải được thiết kế đủ tổng quát để Progress dùng lại mà không phải sửa kiến trúc.

Các quyết định sản phẩm đã chốt:

- Ba tab mở ngay từ đầu.
- Một quest chỉ thuộc một tab.
- Daily và Progress dùng chung Quest Core.
- Food là hệ thống mở khóa/bộ sưu tập riêng, không phải Quest Runtime.
- Daily tự kích hoạt, không có thao tác nhận nhiệm vụ.
- Mỗi Daily Set cấu hình được số task khác nhau.
- Mỗi task tự cấu hình `dailyPoints` và `coinReward`.
- Tổng `dailyPoints` của một Daily Set bắt buộc bằng `100`.
- Ba mốc Daily cố định tại `20`, `60`, `100`.
- Thưởng task tự động trao và hiện popup ngay.
- Thưởng mốc phải bấm nhận; mốc sau được nhận trước mốc trước.
- Mốc chưa nhận sẽ mất khi sang ngày mới.
- Daily Set chạy theo vòng lặp nhiều bộ.
- Thời gian chuẩn tiếp tục dùng `WorldTimeAPI` qua `IServerTimeProvider`.
- Khi chưa đồng bộ được server time, tab Daily bị khóa.
- Múi giờ nghiệp vụ là UTC+7; reset lúc 00:00 giờ Việt Nam.
- Danh sách không cuộn; mỗi page hiển thị tối đa 3 task và đổi page bằng hai nút.
- Có cả objective đếm số lần thu hoạch và objective đếm số lượng vật phẩm thu hoạch.

---

## 2. Hiện trạng dự án

### 2.1. Quest hiện tại

Quest Core hiện có:

- `QuestService` quản lý quest đang active trong RAM.
- `QuestObjectiveRuleRegistry` chọn rule theo loại objective.
- `StateReachedObjectiveRule` xử lý objective đạt trạng thái.
- `QuestProgressApplier` cộng tiến độ và chống đếm trùng bằng `progressKey`.
- `QuestCompletionEvaluator` yêu cầu tất cả objective hoàn thành.
- MessagePipe phát accepted/progress/completed payload.

Các giới hạn cần xử lý:

- Chỉ có `StateReached`.
- State chưa được lưu vào `PlayerData`.
- Một `questId` chỉ accept được một lần trong suốt vòng đời service.
- Runtime state dùng `questId` làm identity nên chưa hỗ trợ lặp cùng definition ở ngày khác.
- Quest hoàn thành bị loại khỏi danh sách active nhưng chưa có cơ chế restore/reset theo feature.
- `HashSet<string>` trong objective progress không phù hợp để serialize bằng `JsonUtility`.
- `FarmQuestTestFlow` chỉ là bridge debug và tự accept toàn bộ catalog.

### 2.2. Farm sau bản cập nhật

Farm đã có các domain event rõ nghĩa:

- `FarmEntityPlantedPayload`
- `FarmEntityCaredPayload`
- `FarmEntityStageChangedPayload`
- `FarmEntityRipePayload`
- `FarmEntityHarvestedPayload`

Đây là nguồn sự kiện production phù hợp cho Quest; không tiếp tục suy diễn hành động từ `FarmSlotChangedPayload`.

Hai điểm kỹ thuật cần chỉnh:

1. `FarmModuleInstaller` hiện mới đăng ký broker cũ, trong khi `RootLifetimeScope` đăng ký lại broker Farm bằng tay. Cần chuyển toàn bộ broker Farm về installer và xóa đăng ký trùng ở Root.
2. `FarmEntityHarvestedPayload` hiện chỉ chứa output đầu tiên dù `FarmService` có thể trao nhiều output. Cần truyền đầy đủ output để objective đếm vật phẩm không bị thiếu.

### 2.3. Time và Storage

- `ServerTimeService` đã triển khai `IServerTimeProvider`.
- `WebTimeSyncSource` gọi `worldtimeapi.org`.
- `IsSynced` chỉ true sau khi sync thành công, phù hợp với yêu cầu khóa Daily.
- `PlayerDataHolder` đang là implementation của `IStorageService`.
- Coin hiện được sửa trực tiếp qua property, chưa có reward transaction ID và chưa phát currency event.
- Save local dùng JSON atomic write nhưng chưa có Daily state và reward ledger.

---

## 3. Kiến trúc tổng thể

```text
Gameplay Modules
    |
    | Domain events: planted, cared, harvested...
    v
Quest Event Bridges
    |
    | QuestProgressEvent chuẩn hóa
    v
Quest Core
    |-- Objective rules
    |-- Runtime state
    |-- Progress application
    `-- Completion evaluation
             |
             | QuestCompletedPayload
             v
Daily Quest Service
    |-- Chọn Daily Set theo ngày
    |-- Restore/reset
    |-- Tính Daily points
    |-- Mở/nhận milestones
    |-- Gọi Reward Service
    `-- Phát state change cho UI
             |
             v
Repository + PlayerData + Reward Ledger
             |
             v
Daily Presenter -> Daily View
```

### Pattern sử dụng

- **Strategy Pattern**: mỗi `QuestObjectiveType` có một `IQuestObjectiveRule`.
- **Registry Pattern**: `QuestObjectiveRuleRegistry` ánh xạ type sang rule.
- **Adapter Pattern**: `FarmQuestEventBridge` chuyển payload Farm sang event chuẩn của Quest.
- **Repository Pattern**: `IDailyQuestRepository` tách Daily logic khỏi `PlayerDataHolder`.
- **Application Service / Coordinator**: `DailyQuestService` điều phối lịch, reset, reward và milestones.
- **Idempotency Key Pattern**: mọi reward có transaction ID ổn định để không trao hai lần.
- **Presenter Pattern**: presenter chuyển state thành view model; MonoBehaviour View không chứa nghiệp vụ.
- **Module Installer Pattern**: mỗi module sở hữu broker và đăng ký DI của chính nó.

---

## 4. Thiết kế Quest Core

### 4.1. Tách loại objective khỏi loại event

Không tiếp tục dùng `QuestObjectiveType` đồng thời làm loại objective và event.

Thêm `QuestEventType`:

- `FarmPlanted`
- `FarmCared`
- `FarmHarvestAction`
- `FarmHarvestItem`
- `FarmRipe`
- `FarmStageReached`
- Dự phòng cho Progress: `InventoryChanged`, `CurrencyChanged`, `FeatureUnlocked`.

`QuestObjectiveType` gồm:

- `ActionCount`: cộng theo số lần hành động; event adapter luôn gửi amount `1`.
- `ItemAmount`: cộng theo số lượng vật phẩm trong event.
- `StateReached`: hoàn thành/cộng tiến độ khi target đạt state xác định.

### 4.2. Target matching

Thêm `QuestTargetScope`:

- `Any`: nhận mọi target cùng event type.
- `ExactTarget`: so khớp `targetId`.
- `TargetCategory`: so khớp category như `Crop` hoặc `Animal`.

`QuestObjectiveData` sẽ chứa:

- `objectiveId`
- `objectiveType`
- `eventType`
- `targetScope`
- `targetId`
- `targetCategory`
- `targetState`
- `requiredAmount`

Rule chỉ chịu trách nhiệm:

1. Kiểm tra event type.
2. Kiểm tra target theo scope.
3. Áp dụng progress theo semantics của objective.

### 4.3. Quest event chuẩn

`QuestProgressEvent` cần chứa:

- `EventType`
- `TargetId`
- `TargetCategory`
- `State`
- `Amount`
- `ProgressKey`

Quy tắc:

- `Amount` không được tự ép tối thiểu thành 1 ở constructor chung.
- `ActionCountObjectiveRule` sử dụng `1`.
- `ItemAmountObjectiveRule` yêu cầu `Amount > 0`.
- `ProgressKey` bắt buộc với event hành động để chống đếm lặp.

### 4.4. Runtime identity

Tách `RuntimeId` khỏi `QuestDefinitionId`.

Ví dụ:

```text
DefinitionId: daily_harvest_crop
RuntimeId: daily:2026-07-23:daily_harvest_crop
```

Nhờ đó cùng một Quest Definition có thể xuất hiện lại ở ngày khác mà không xung đột state cũ.

`QuestRuntimeState` gồm:

- `RuntimeId`
- `QuestDefinitionId`
- `QuestStatus`
- Danh sách objective progress
- Lookup runtime không serialize

### 4.5. API của Quest Service

`IQuestService` được chuyển thành API runtime tổng quát:

- `ActivateQuest(runtimeId, definitionId, snapshot = null)`
- `DeactivateQuest(runtimeId)`
- `DeactivateByPrefix(prefix)`
- `ReportEvent(progressEvent)`
- `GetQuestState(runtimeId)`
- `GetActiveQuests()`
- `CreateSnapshot(runtimeId)`

Behavior:

- Activate mới tạo state rỗng.
- Activate với snapshot restore objective progress.
- Activate trùng runtime ID là idempotent, không reset state.
- Deactivate chỉ bỏ theo dõi; không trao thưởng và không xóa save.
- Quest Core chỉ phát completed; không quyết định reward.

### 4.6. Objective rules

Giữ `QuestProgressApplier` làm helper duy nhất thay đổi progress.

Các rule:

- `ActionCountObjectiveRule`: target match, cộng 1.
- `ItemAmountObjectiveRule`: target match, cộng `event.Amount`.
- `StateReachedObjectiveRule`: target và state match, áp dụng amount theo cấu hình hiện tại.

`QuestProgressApplier`:

- Clamp tại `requiredAmount`.
- Không thay đổi objective đã complete.
- Không đếm lại `ProgressKey`.
- Runtime dùng `HashSet`; snapshot dùng `List<string>`.

---

## 5. ScriptableObject cho Daily

### 5.1. Quest Definition

`QuestDefinitionSO` giữ dữ liệu dùng chung:

- `questId`
- `category`: `Daily` hoặc `Progress`
- `displayName`
- `description`
- `icon`
- `objectives`

Không đặt Daily points hay coin reward vào đây.

### 5.2. Daily Quest Entry

`DailyQuestEntry` là serializable entry trong một set:

- `QuestDefinitionSO quest`
- `int dailyPoints`
- `int coinReward`

Lợi ích:

- Cùng một Daily Quest có thể được tái sử dụng ở nhiều set.
- Designer thay đổi điểm/thưởng theo độ khó của từng set.
- Quest Core không biết reward.

### 5.3. Daily Milestone

`DailyMilestoneDefinition`:

- `int requiredPoints`
- `int coinReward`

Mỗi set phải có đúng ba mốc:

- 20
- 60
- 100

### 5.4. Daily Quest Set

`DailyQuestSetSO`:

- `string setId`
- `List<DailyQuestEntry> tasks`
- `List<DailyMilestoneDefinition> milestones`

Không giới hạn cứng số task, nhưng:

- Phải có ít nhất một task.
- Không được trùng quest trong cùng set.
- Tổng `dailyPoints` phải bằng 100.
- `dailyPoints > 0`.
- `coinReward >= 0`.

### 5.5. Daily Schedule

`DailyQuestScheduleSO`:

- `string cycleStartDate`, định dạng `yyyy-MM-dd`, hiểu theo UTC+7.
- Danh sách `DailyQuestSetSO` có thứ tự.

Chọn set:

```text
vietnamDate = Date(IServerTimeProvider.UtcNow + 07:00)
dayOffset = vietnamDate - cycleStartDate
setIndex = positiveModulo(dayOffset, setCount)
dailySet = sets[setIndex]
```

State ngày hiện tại lưu `setId`; nếu designer đổi thứ tự set giữa ngày, người đang chơi vẫn restore set đã lưu cho đến lần reset tiếp theo.

Nếu `setId` đã lưu không còn tồn tại:

- Đánh dấu config error.
- Khóa Daily.
- Không tự thay bằng set khác giữa cùng một ngày.

---

## 6. Validation dành cho designer

Tạo validator chạy trong Editor và từ menu `Tools/Quest/Validate Daily Content`.

Validator kiểm tra:

- Quest ID và set ID không rỗng, không trùng.
- Quest trong Daily Set có category `Daily`.
- Quest Definition tồn tại trong catalog.
- Objective ID không rỗng và không trùng trong quest.
- Rule tương ứng với objective type tồn tại.
- Exact target phải có target ID.
- Category target phải có category.
- Required amount lớn hơn 0.
- Set có ít nhất một task.
- Tổng điểm set đúng 100.
- Mốc đúng 20/60/100 và không trùng.
- Schedule có cycle start date hợp lệ và ít nhất một set.
- Không có null reference.

Build development phải log lỗi rõ set/quest/objective nào sai. Production không tự chạy với content invalid.

---

## 7. Tích hợp Farm với Quest

### 7.1. Broker ownership

`FarmModuleInstaller.RegisterFarmEvents()` phải đăng ký toàn bộ:

- `FarmSlotChangedPayload`
- `OpenFarmSelectorUIPayload`
- `FarmEntityPlantedPayload`
- `FarmEntityCaredPayload`
- `FarmEntityStageChangedPayload`
- `FarmEntityRipePayload`
- `FarmEntityHarvestedPayload`

`RootLifetimeScope` chỉ gọi `RegisterFarmModule(options)`; xóa toàn bộ đăng ký broker Farm/Time/Storage/Quest bị lặp bằng tay.

### 7.2. Harvest payload

Thay output đơn bằng danh sách:

`FarmHarvestOutput`:

- `ItemId`
- `Amount`

`FarmEntityHarvestedPayload`:

- `EventId`
- `EntityId`
- `Cell`
- `EntityType`
- `IReadOnlyList<FarmHarvestOutput> Outputs`

`FarmService.TryHarvest()`:

- Tạo một `EventId` cho lần thu hoạch thành công.
- Thu thập toàn bộ output đã thực sự đưa vào kho.
- Publish đúng một harvested payload sau khi cập nhật kho và state.

### 7.3. Event ID

Các payload hành động `Planted`, `Cared`, `Harvested` cần `EventId` duy nhất.

Event ID chỉ được tạo khi hành động thành công. Failed action không publish event.

### 7.4. Farm Quest Event Bridge

`FarmQuestEventBridge` subscribe các payload production:

| Farm payload | Quest event | Amount | Progress key |
|---|---|---:|---|
| Planted | FarmPlanted | 1 | `{eventId}:plant` |
| Cared | FarmCared | 1 | `{eventId}:care` |
| Harvested | FarmHarvestAction | 1 | `{eventId}:harvest` |
| Harvested output | FarmHarvestItem | output amount | `{eventId}:item:{itemId}` |
| Ripe | FarmRipe | 1 | event-specific key |
| StageChanged | FarmStageReached | 1 | event-specific key |

Ví dụ:

- “Thu hoạch 10 lần cây trồng”: `ActionCount + FarmHarvestAction + category Crop`.
- “Thu hoạch 10 lúa”: `ItemAmount + FarmHarvestItem + exact target wheat_grain`.
- “Thu hoạch 5 sữa”: `ItemAmount + FarmHarvestItem + exact target milk`.
- “Cho 10 con vật ăn”: `ActionCount + FarmCared + category Animal`.

`FarmQuestTestFlow` không còn tự accept catalog. Thay bằng bridge production; phần debug chỉ được phép tạo quest test khi người dùng chủ động bật.

---

## 8. Daily runtime và reset

### 8.1. Daily availability

`DailyAvailabilityState`:

- `WaitingForServerTime`
- `Ready`
- `ConfigurationError`

UI khóa interaction nếu state không phải Ready.

### 8.2. Khởi tạo

Daily chỉ initialize khi:

- PlayerData đã load.
- `IServerTimeProvider.IsSynced == true`.
- Schedule hợp lệ.

Flow:

```text
PlayerData loaded
        +
ServerTime synced
        |
        v
Tính dayKey UTC+7
        |
        v
Chọn Daily Set theo vòng lặp
        |
        +-- Save cùng dayKey/setId -> restore
        |
        `-- Ngày mới/không có save -> tạo state mới
        |
        v
Activate tất cả quest với RuntimeId của ngày
        |
        v
Reconcile reward còn pending
        |
        v
Daily Ready
```

### 8.3. Theo dõi đổi ngày

`DailyQuestService` subscribe `ClockTickPayload`.

Mỗi tick:

- Nếu time chưa synced: không reset.
- Tính day key UTC+7.
- Nếu day key không đổi: chỉ cập nhật countdown.
- Nếu day key đổi: chạy reset một lần.

Khi `ServerTimeSyncedPayload` đến sau resync:

- Tính lại day key ngay.
- Nếu offset mới làm đổi ngày, reset ngay theo server date.

### 8.4. Reset

Khi sang ngày mới:

1. Khóa tạm Daily UI.
2. Deactivate tất cả runtime có prefix của ngày cũ.
3. Bỏ task progress và milestone state cũ.
4. Không tự trao milestone chưa nhận.
5. Chọn set mới.
6. Tạo runtime ID mới.
7. Save state ngày mới ngay lập tức.
8. Mở lại Daily UI và chuyển về page 0.

### 8.5. Progress state

Daily task state lưu:

- `runtimeId`
- `questDefinitionId`
- `status`
- Objective snapshots

Daily points không được tăng mù. Giá trị chuẩn luôn được tính lại:

```text
totalPoints = tổng dailyPoints của các task Completed
```

Nhờ đó completion event bị gọi lại cũng không cộng trùng.

---

## 9. Reward và chống trao trùng

### 9.1. Reward transaction ID

Task reward:

```text
daily:{dayKey}:task:{questDefinitionId}
```

Milestone reward:

```text
daily:{dayKey}:milestone:{requiredPoints}
```

### 9.2. Reward Service

Thêm `IRewardService`:

- `GrantCoins(transactionId, amount, source)`
- Trả `Granted`, `AlreadyGranted` hoặc `Failed`.

`PlayerData` thêm danh sách transaction ID đã grant.

`PlayerDataHolder`:

- Kiểm tra transaction ID.
- Nếu đã có: trả AlreadyGranted, không cộng coin.
- Nếu chưa có: cộng coin, thêm transaction ID, save immediate.
- Sau khi save thành công mới publish reward/currency event.

Đổi `SaveImmediate()` trả kết quả thành công/thất bại để caller không đánh dấu reward đã xử lý khi ghi file lỗi.

### 9.3. Hoàn thành task

```text
QuestCompletedPayload
-> Daily kiểm tra runtime có thuộc ngày hiện tại không
-> Persist task Completed
-> Tính lại totalPoints
-> Save Daily state
-> GrantCoins với transaction ID ổn định
-> Nếu Granted: publish popup event
-> Nếu AlreadyGranted: không popup lại
-> Nếu Failed: giữ task Completed; reconcile ở lần save/init tiếp theo
```

### 9.4. Nhận milestone

```text
Người chơi bấm mốc
-> Kiểm tra Daily Ready
-> Kiểm tra totalPoints >= requiredPoints
-> Kiểm tra milestone chưa Claimed
-> GrantCoins(transaction ID milestone)
-> Granted hoặc AlreadyGranted: đánh dấu Claimed và save
-> Failed: giữ Claimable
```

Mốc sau không phụ thuộc trạng thái claimed của mốc trước.

---

## 10. Persistence và migration

### 10.1. Save model

`DailyQuestSaveData`:

- `schemaVersion`
- `dayKey`
- `setId`
- `List<DailyQuestTaskSaveData>`
- `List<DailyMilestoneSaveData>`

`DailyQuestTaskSaveData`:

- `runtimeId`
- `questDefinitionId`
- `status`
- `List<QuestObjectiveProgressSnapshot>`

`QuestObjectiveProgressSnapshot`:

- `objectiveId`
- `currentAmount`
- `isCompleted`
- `List<string> countedProgressKeys`

`DailyMilestoneSaveData`:

- `requiredPoints`
- `claimed`

`PlayerData` thêm:

- `DailyQuestSaveData DailyQuest`
- `List<string> GrantedRewardTransactionIds`

### 10.2. Repository

`IDailyQuestRepository` thuộc Quest assembly:

- `bool IsLoaded`
- `DailyQuestSaveData Load()`
- `bool Save(DailyQuestSaveData data, bool immediate)`
- `void Clear()`

`PlayerDataDailyQuestRepository` thuộc MyOwn assembly:

- Map repository sang `PlayerDataHolder.Data`.
- Không chứa rule chọn set/reset/reward.
- Save immediate cho completion, reset và claim.
- Save throttled cho progress tăng nhưng chưa hoàn thành.

### 10.3. Migration

- Tăng `PlayerData.SaveVersion`.
- Save cũ không có Daily data được xem như chưa khởi tạo Daily.
- Inventory, coin và FarmSlots cũ được giữ nguyên.
- Không cố migrate state từ Quest debug hiện tại vì state đó chưa được lưu.
- Danh sách reward transaction khởi tạo rỗng.

---

## 11. UI và UIManager

### 11.1. Cấu trúc

Dùng một `QuestWindow` của Bruno Mikoski UIManager trên layer Popup.

```text
QuestWindow
|-- Header
|-- CloseButton
|-- Tabs
|   |-- DailyButton
|   |-- ProgressButton
|   `-- FoodButton
|-- DailyPanel
|   |-- LockedOverlay
|   |-- DailyPointBar
|   |-- Milestones (20/60/100)
|   |-- TaskContainer
|   |-- PreviousPageButton
|   |-- PageLabel
|   |-- NextPageButton
|   `-- ResetCountdown
|-- ProgressPanel
`-- FoodPanel
```

Progress và Food giai đoạn đầu là placeholder panel, không chứa logic giả.

### 11.2. Phân trang

- `ItemsPerPage = 3`.
- `pageCount = ceil(taskCount / 3)`.
- Page index bắt đầu từ 0 trong code, hiển thị từ 1 trên UI.
- Page đầu disable nút Previous.
- Page cuối disable nút Next.
- Khi reset sang set có ít page hơn, clamp về page hợp lệ; mặc định reset về page đầu.
- Không instantiate lại toàn bộ list mỗi lần đổi page; dùng pool cố định 3 item view và bind lại.

### 11.3. Presenter

`DailyQuestPresenter`:

- Đọc snapshot từ `IDailyQuestService`.
- Tạo view model cho ba task của page hiện tại.
- Bind point bar, milestone, countdown và button states.
- Subscribe Daily state changed payload.
- Không trao coin, cộng điểm hoặc reset state.

### 11.4. Reward popup

`DailyRewardPopupQueue`:

- Subscribe reward granted payload.
- Nếu nhiều task hoàn thành từ cùng một event, xếp popup vào queue.
- Popup đầu hiển thị ngay; các popup sau hiển thị tuần tự.
- Popup không điều khiển reward; reward đã commit trước khi popup được enqueue.

### 11.5. Window lifecycle

`QuestWindowController` kế thừa `WindowController`:

- `IOnBeforeWindowOpen`: bind presenter và mở tab mặc định/last selected.
- `IOnWindowClosed`: unbind listener UI.
- Nút close gọi `Close()`.

`QuestWindowLauncher`:

- Nhận Button, `WindowsManager`, `UIWindow`.
- Mở window qua UIManager.
- Lấy instance và inject bằng VContainer theo pattern đang dùng ở `FarmUIBridge`.

### 11.6. Editor setup tool

Cập nhật `QuestUiSetupTool`:

- Tạo đúng hierarchy ba tab và Daily panel.
- Tạo 3 task item slot cố định.
- Tạo 3 milestone view.
- Tạo nút page và label.
- Không tạo dữ liệu runtime mẫu trong production hierarchy.
- Không ghi đè object có sẵn; báo rõ nếu hierarchy đã tồn tại.

Artwork, font, sprite và màu vẫn được designer gán trong prefab/Inspector.

---

## 12. Trách nhiệm từng file

### Quest Core

- `QuestEventType.cs`: danh sách event chuẩn mà objective có thể nghe.
- `QuestObjectiveType.cs`: semantics ActionCount/ItemAmount/StateReached.
- `QuestTargetScope.cs`: Any/ExactTarget/TargetCategory.
- `QuestObjectiveData.cs`: cấu hình event, target và required amount.
- `QuestProgressEvent.cs`: DTO event chuẩn từ các bridge.
- `QuestRuntimeState.cs`: state runtime theo RuntimeId.
- `QuestObjectiveProgress.cs`: progress runtime và dedupe set.
- `QuestRuntimeSnapshot.cs`: dữ liệu serializable để repository lưu.
- `ActionCountObjectiveRule.cs`: cộng một lần cho mỗi action event.
- `ItemAmountObjectiveRule.cs`: cộng số lượng item.
- `StateReachedObjectiveRule.cs`: giữ behavior state objective.
- `QuestProgressApplier.cs`: mutation progress duy nhất.
- `QuestCompletionEvaluator.cs`: kiểm tra toàn bộ objective.
- `QuestObjectiveRuleRegistry.cs`: resolve strategy theo objective type.
- `IQuestService.cs`: contract activate/deactivate/report/query/snapshot.
- `QuestService.cs`: engine runtime, không chứa Daily/reward/UI.

### Daily

- `DailyQuestEntry.cs`: quest reference, daily points, coin reward.
- `DailyMilestoneDefinition.cs`: threshold và coin reward.
- `DailyQuestSetSO.cs`: một bộ Daily có số task linh hoạt.
- `DailyQuestScheduleSO.cs`: anchor date và ordered cycle.
- `DailyQuestSaveData.cs`: root save model Daily.
- `IDailyQuestRepository.cs`: persistence abstraction.
- `IDailyQuestService.cs`: API initialize/query/claim/page-independent.
- `DailyQuestService.cs`: lifecycle, reset, restore, completion, points, milestones.
- `DailyQuestScheduleResolver.cs`: tính set index từ server day.
- `DailyQuestValidator.cs`: validation runtime dùng chung.
- `DailyQuestStateChangedPayload.cs`: yêu cầu UI refresh.
- `DailyQuestRewardGrantedPayload.cs`: dữ liệu popup.
- `DailyAvailabilityChangedPayload.cs`: lock/ready/config error.

### Integration

- `FarmQuestEventBridge.cs`: chuyển payload Farm thành QuestProgressEvent.
- `DailyQuestBootstrapper.cs`: chờ PlayerData + server time rồi initialize Daily.
- `PlayerDataDailyQuestRepository.cs`: adapter từ Daily repository sang PlayerData.

### Storage và app data

- `IRewardService.cs`: contract grant idempotent bằng transaction ID.
- `RewardGrantResult.cs`: Granted/AlreadyGranted/Failed.
- `CurrencyChangedPayload.cs`: cập nhật UI coin.
- `RewardGrantedPayload.cs`: event reward tổng quát.
- `PlayerData.cs`: thêm Daily save và reward transaction ledger.
- `PlayerDataHolder.cs`: implement reward service và save result.
- `PlayerDataSaveLoad.cs`: giữ atomic write, trả success/failure.

### Farm

- `FarmEntityHarvestedPayload.cs`: chứa EventId và toàn bộ outputs.
- `FarmEntityPlantedPayload.cs`: thêm EventId.
- `FarmEntityCaredPayload.cs`: thêm EventId.
- `FarmService.cs`: tạo event ID và publish payload sau action thành công.
- `FarmModuleInstaller.cs`: sở hữu toàn bộ Farm brokers.

### UI

- `QuestWindowController.cs`: lifecycle cửa sổ Quest.
- `QuestWindowLauncher.cs`: mở Quest bằng UIManager và inject instance.
- `DailyQuestPresenter.cs`: state-to-view-model và pagination.
- `DailyQuestTabView.cs`: references UI của tab Daily.
- `DailyQuestItemView.cs`: render một task.
- `DailyMilestoneView.cs`: render/click một milestone.
- `DailyRewardPopupQueue.cs`: hiển thị popup tuần tự.
- `QuestUiSetupTool.cs`: tạo hierarchy static trong Editor.

### Bootstrap

- `QuestModuleInstaller.cs`: đăng ký Quest Core, Daily, rules, bridge và brokers.
- `RootLifetimeScope.cs`: chỉ compose module installers và app adapters, không đăng ký broker module trùng.
- `GameLifetimeScope.cs`: đăng ký UI scene components nếu chúng nằm trong scene.

---

## 13. API Daily cho UI

`IDailyQuestService` cung cấp read-only snapshot:

- Availability state.
- Day key.
- Set ID.
- Total points.
- Time remaining đến reset.
- Ordered task snapshots.
- Ordered milestone snapshots.

Commands:

- `TryClaimMilestone(requiredPoints)`
- Không có `AcceptQuest`.
- Không có command đổi page; page là presentation state.

Payload state changed chỉ báo “state đã đổi”; UI query snapshot mới thay vì payload mang toàn bộ mutable state.

---

## 14. Trình tự triển khai

### Giai đoạn 1: Làm sạch module wiring

1. Di chuyển broker ownership về đúng module installer.
2. Xóa broker registrations bị trùng trong Root.
3. Cập nhật Farm harvested payload để giữ toàn bộ outputs.
4. Bổ sung EventId và test Farm events.

### Giai đoạn 2: Refactor Quest Core

1. Tách event type khỏi objective type.
2. Thêm target matching.
3. Thêm ActionCount và ItemAmount rule.
4. Chuyển runtime identity sang RuntimeId.
5. Thêm snapshot/restore/deactivate.
6. Cập nhật payload Quest mang RuntimeId và DefinitionId.
7. Giữ toàn bộ test cũ dưới behavior tương đương.

### Giai đoạn 3: Farm bridge

1. Subscribe domain events mới.
2. Map action-count và item-amount events.
3. Dedupe bằng EventId.
4. Xóa auto-accept production khỏi test flow.

### Giai đoạn 4: Daily data và schedule

1. Tạo Entry/Set/Schedule SO.
2. Tạo resolver vòng lặp theo UTC+7.
3. Tạo validator.
4. Tạo content mẫu đủ tổng 100 điểm.

### Giai đoạn 5: Persistence và reward

1. Thêm Daily save schema.
2. Thêm repository adapter.
3. Thêm idempotent reward ledger.
4. Bump save version và migration.
5. Kiểm thử crash/reload bằng các điểm ngắt flow.

### Giai đoạn 6: Daily application service

1. Chờ data/time readiness.
2. Initialize/restore/reset.
3. Activate task tự động.
4. Xử lý completion và reward.
5. Tính points từ completed states.
6. Claim milestone.
7. Reconcile pending reward.

### Giai đoạn 7: UI

1. Tạo Quest window bằng UIManager.
2. Tạo Daily tab và ba item view.
3. Bind point bar/milestone/countdown.
4. Implement pagination.
5. Implement locked state.
6. Implement reward popup queue.
7. Cập nhật editor setup tool.

### Giai đoạn 8: Content và nghiệm thu

1. Designer tạo nhiều Daily Set.
2. Validate toàn bộ content.
3. Chạy EditMode tests.
4. Chạy PlayMode integration tests.
5. Test trên build với WorldTimeAPI thật.

---

## 15. Kế hoạch kiểm thử

### Quest Core EditMode

- Activate mới và activate trùng.
- Restore snapshot đúng progress.
- Deactivate không nhận event nữa.
- Cùng definition chạy với hai RuntimeId độc lập.
- ActionCount luôn cộng 1.
- ItemAmount cộng đúng sản lượng.
- Exact target/category/any match đúng.
- Duplicate progress key không cộng lại.
- Quest completed chỉ phát một lần.
- Snapshot HashSet/List round-trip đúng.

### Daily Schedule

- Anchor date chọn set index 0.
- Ngày kế tiếp chọn set kế tiếp.
- Hết danh sách quay về set đầu.
- UTC+7 đổi ngày đúng lúc 17:00 UTC.
- Positive modulo an toàn nếu test ngày trước anchor.
- Số task khác nhau không ảnh hưởng chọn set.

### Daily Service

- Chưa sync time => WaitingForServerTime.
- Sync thành công => Ready.
- Save cùng ngày => restore.
- Ngày mới => reset.
- Set variable task count activate đủ task.
- Completion tính lại points đúng.
- Event completion lặp không cộng điểm.
- Mốc 20/60/100 chuyển Locked/Claimable đúng.
- Nhận mốc sau trước mốc trước thành công.
- Mốc chưa nhận mất khi reset.
- Config invalid => ConfigurationError.

### Reward

- Task reward grant đúng một lần.
- Milestone reward grant đúng một lần.
- Spam button không nhân coin.
- Reload sau grant không grant lại.
- Completed save trước, reward fail => reconcile grant sau.
- AlreadyGranted không hiện popup lại.
- Save failure không báo Claimed giả.

### Farm bridge

- Plant crop/animal map đúng category.
- Care animal tăng care objective.
- Harvest action tăng 1 dù output amount lớn.
- Harvest item tăng theo amount.
- Multi-output tạo một action event và nhiều item event.
- Failed Farm action không tạo quest progress.

### UI PlayMode

- 1–3 task => một page.
- 4–6 task => hai page.
- 7 task trở lên => số page động.
- Previous/Next disable đúng.
- Reset về page đầu.
- Locked overlay chặn claim/page interaction cần chặn.
- Popup nhiều reward chạy tuần tự.
- Đóng/mở window không đăng ký listener trùng.

### Regression

- Farm planting/feeding/harvesting vẫn hoạt động.
- Farm visual vẫn dùng FarmSlotChangedPayload.
- Save cũ load được.
- Clock/Farm offline progress không bị ảnh hưởng.
- Quest debug tests cũ được cập nhật và vẫn pass.

---

## 16. Tiêu chí hoàn thành

Daily v1 được xem là hoàn thành khi:

- Daily chỉ mở sau server time sync.
- Set chọn đúng theo vòng lặp và UTC+7.
- Mỗi set có số task linh hoạt, tổng điểm đúng 100.
- Tất cả task tự active.
- Farm events tăng đúng objective.
- Có thể phân biệt harvest action và harvest item amount.
- Progress persist qua scene/restart.
- Reset đúng ngày và mất milestone chưa nhận.
- Task reward tự động, popup ngay và không grant trùng.
- Milestone claim thủ công, thứ tự tự do và không grant trùng.
- UI hiển thị tối đa 3 task/page, không scroll.
- Module installer không đăng ký broker trùng.
- EditMode và PlayMode tests liên quan đều pass.

---

## 17. Phạm vi để sau

Không triển khai trong Daily v1:

- Progress quest chain và Progress milestones.
- Food unlock/catalog runtime.
- Random Daily.
- Reroll Daily.
- Cloud save Daily.
- Bù nhiệm vụ cho ngày bỏ lỡ.
- Tự trao milestone chưa nhận.
- Notification ngoài popup reward.

Quest Core, reward ledger và persistence contracts được chuẩn bị để các giai đoạn này dùng lại.
