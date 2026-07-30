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
- Danh sách không cuộn; mỗi page hiển thị tối đa 2 task và đổi page bằng hai nút.
- Có cả objective đếm số lần thu hoạch và objective đếm số lượng vật phẩm thu hoạch.
- Daily v1 chỉ đếm hành động chủ động của người chơi: trồng, chăm sóc/cho ăn và thu hoạch. `Ripe`/`StageReached` chưa dùng cho Daily v1.
- Có 3 Daily Set mẫu, mỗi set 4 task; mặc định 25 điểm/task, 100 coin/task và milestone thưởng 100/300/500 coin.
- Daily dùng UI động ghép từ texture rời. Progress và Food chỉ là panel mockup tĩnh; tab chuyển được nhưng chưa có logic runtime.
- Reward task phải được xử lý ngay trong phiên hiện tại. Reconcile khi khởi động chỉ là lớp an toàn nếu game bị crash giữa transaction.

### 1.1. Constraint ownership đã chốt

- Không sửa hoặc xóa broker registration hiện có trong `RootLifetimeScope`.
- Không sửa code thuộc Farm module, gồm payload, `FarmService` và `FarmModuleInstaller`.
- Quest chỉ subscribe các Farm payload hiện có và thích nghi ở `FarmQuestEventBridge`.
- Được phép mở rộng `PlayerData`, `PlayerDataHolder`, `PlayerDataSaveLoad` và `IStorageService`, nhưng phải giữ nguyên behavior mà Farm đang sử dụng.
- Broker registration trùng của module khác được ghi nhận là technical debt ngoài phạm vi Quest; không thêm đăng ký trùng mới.

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

Tình trạng đã trace:

- `FarmEntityHarvestedPayload` đã chứa `OutputReward[] Outputs` và test Farm đã kiểm tra multi-output.
- `Planted`, `Cared` và `Harvested` chưa có `EventId`.
- `Outputs` hiện dùng type cấu hình Farm có reference tới `ItemDataSO`; bridge phải map sang `ItemId` và `amount`, đồng thời bỏ entry null/invalid.
- `FarmModuleInstaller` đã đăng ký các Farm broker, nhưng Root vẫn đăng ký lại. Theo constraint ownership, Quest không sửa hai vị trí này.
- Vì không được sửa Farm payload, Quest không thể có idempotency key bền vững từ producer. Bridge chỉ có thể chống callback trùng trong cùng frame bằng fingerprint tạm thời; đây là giới hạn đã chấp nhận của Daily v1.

### 2.3. Time và Storage

- `ServerTimeService` đã triển khai `IServerTimeProvider`.
- `WebTimeSyncSource` gọi `worldtimeapi.org`.
- `IsSynced` chỉ true sau khi sync thành công, phù hợp với yêu cầu khóa Daily.
- `PlayerDataHolder` đang là implementation của `IStorageService`.
- Coin hiện được sửa trực tiếp qua property, chưa có reward transaction ID và chưa phát currency event.
- Save local dùng JSON atomic write nhưng chưa có Daily state và reward ledger.
- Save throttled hiện đưa cùng object `PlayerData` mutable sang thread pool; `SaveImmediate()` và `PlayerDataSaveLoad.Save()` chưa trả kết quả.
- Quest cần một single-writer gate để throttled save và immediate save không ghi file đồng thời, đồng thời cần snapshot ổn định khi ghi.

### 2.4. Loading, Addressables và DI scope

- `QuestCatalogSO` được `QuestCatalogProvider : IBootPreloader` load qua Addressables trong Preloading scene.
- `MapSceneBootstrap` lấy catalog từ Root, enqueue vào `GameLifetimeScope`, sau đó mới build Quest gameplay services và mở `_gameplayRoot`.
- Quest brokers và catalog provider sống ở Root scope; `QuestService`, rules, Daily service và Farm bridge sống ở Game scope.
- `PlayerDataHolder` và `ServerTimeService` khởi động ở Root nên tín hiệu ready có thể đã xảy ra trước khi Game scope tồn tại.
- Daily bootstrap bắt buộc dùng pattern **check-current-state-or-wait**; repository cung cấp `IsLoaded/WaitUntilLoadedAsync`, còn time dùng `IsSynced/ServerTimeSyncedPayload`.
- Quest assembly không reference `PlayerDataLoadedPayload` của MyOwn để tránh dependency cycle `Quest -> MyOwn -> Quest`.
- `DailyQuestScheduleSO` sẽ được reference từ `QuestCatalogSO`, nhờ đó đi theo dependency graph Addressables hiện có mà không thêm hard reference mới vào Root.

---

## 3. Kiến trúc tổng thể

```text
Preloading scene / Root scope
    |-- PlayerDataHolder
    |-- ServerTimeService
    |-- QuestCatalogProvider -> Addressables QuestCatalogSO
    `-- Quest-owned MessagePipe brokers
             |
             | MapSceneBootstrap enqueue QuestCatalogSO
             v
Game scope / Gameplay Modules
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
Daily Presenter -> QuestWindow
                    |-- Daily dynamic panel
                    |-- Progress static placeholder
                    `-- Food static placeholder
```

### Pattern sử dụng

- **Strategy Pattern**: mỗi `QuestObjectiveType` có một `IQuestObjectiveRule`.
- **Registry Pattern**: `QuestObjectiveRuleRegistry` ánh xạ type sang rule.
- **Adapter Pattern**: `FarmQuestEventBridge` chuyển payload Farm sang event chuẩn của Quest.
- **Repository Pattern**: `IDailyQuestRepository` tách Daily logic khỏi `PlayerDataHolder`.
- **Application Service / Coordinator**: `DailyQuestService` điều phối lịch, reset, reward và milestones.
- **Idempotency Key Pattern**: mọi reward có transaction ID ổn định để không trao hai lần.
- **Presenter Pattern**: presenter chuyển state thành view model; MonoBehaviour View không chứa nghiệp vụ.
- **Module Installer Pattern**: Quest sở hữu broker/DI của Quest. Broker trùng ở module khác là constraint ngoài phạm vi và không được Quest sửa.

---

## 4. Thiết kế Quest Core

### 4.1. Tách loại objective khỏi loại event

Không tiếp tục dùng `QuestObjectiveType` đồng thời làm loại objective và event.

Thêm `QuestEventType`:

- `FarmPlanted`
- `FarmCared`
- `FarmHarvestAction`
- `FarmHarvestItem`
- Dự phòng, chưa dùng trong Daily v1: `FarmRipe`, `FarmStageReached`.
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
- `DeactivateQuests(IEnumerable<string> runtimeIds)`
- `ReportEvent(progressEvent)`
- `GetQuestState(runtimeId)`
- `GetActiveQuests()`
- `CreateSnapshot(runtimeId)`

Behavior:

- Activate mới tạo state rỗng.
- Activate với snapshot restore objective progress.
- Activate trùng runtime ID là idempotent, không reset state.
- Restore snapshot Completed giữ state để Daily tính điểm nhưng không đưa quest trở lại event-processing list và không phát completed lần nữa.
- Deactivate chỉ bỏ theo dõi; không trao thưởng và không xóa save.
- Quest Core chỉ phát completed; không quyết định reward.
- Quest Core không biết convention prefix của Daily; Daily giữ chính xác danh sách RuntimeId cần deactivate.

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
- `int contentVersion`.
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

Quy tắc content update:

- `setId` và `questId` đã phát hành là ID bất biến.
- Không xóa hoặc đổi nghĩa set đang có thể được người chơi restore trong ngày hiện tại.
- Daily v1 chưa hỗ trợ hot-swap set giữa ngày.
- Thay đổi thứ tự chỉ ảnh hưởng lần chọn set ở ngày mới; save cùng ngày tiếp tục dùng `setId` đã lưu.

### 5.6. Tích hợp với Quest Catalog và content mẫu

Mở rộng `QuestCatalogSO`:

- Giữ danh sách `QuestDefinitionSO` hiện có.
- Thêm reference tới `DailyQuestScheduleSO`.
- `QuestCatalogSO` tiếp tục là Addressable root được `QuestCatalogProvider` preload.
- Schedule, Daily Set, Quest Definition và icon là dependency của catalog; không tạo loader song song.

Content v1:

- Tạo 3 Daily Set mẫu.
- Mỗi set có 4 task, tương ứng 2 page.
- Mặc định mỗi task 25 Daily points và 100 coin.
- Ba milestone mặc định thưởng lần lượt 100, 300 và 500 coin.
- Designer vẫn có thể chỉnh điểm/reward từng entry miễn validator xác nhận tổng điểm set bằng 100.

Content mẫu ban đầu:

| Set | Task | Objective | Required | Points | Coin |
|---|---|---|---:|---:|---:|
| `daily_set_01` | Trồng cây | `ActionCount + FarmPlanted + category Crop` | 4 | 25 | 100 |
|  | Cho vật nuôi ăn | `ActionCount + FarmCared + category Animal` | 2 | 25 | 100 |
|  | Thu hoạch | `ActionCount + FarmHarvestAction + Any` | 4 | 25 | 100 |
|  | Thu hoạch lúa | `ItemAmount + FarmHarvestItem + wheat_grain` | 8 | 25 | 100 |
| `daily_set_02` | Trồng mía | `ActionCount + FarmPlanted + c_sugarcane` | 3 | 25 | 100 |
|  | Chăm sóc | `ActionCount + FarmCared + Any` | 3 | 25 | 100 |
|  | Thu hoạch cây trồng | `ActionCount + FarmHarvestAction + category Crop` | 4 | 25 | 100 |
|  | Thu hoạch mía | `ItemAmount + FarmHarvestItem + sugarcane_raw` | 6 | 25 | 100 |
| `daily_set_03` | Nuôi gà | `ActionCount + FarmPlanted + a_chicken` | 2 | 25 | 100 |
|  | Cho vật nuôi ăn | `ActionCount + FarmCared + category Animal` | 4 | 25 | 100 |
|  | Thu hoạch vật nuôi | `ActionCount + FarmHarvestAction + category Animal` | 3 | 25 | 100 |
|  | Thu hoạch trứng | `ItemAmount + FarmHarvestItem + egg` | 3 | 25 | 100 |

Required amount là content tuning ban đầu, không hard-code trong service.

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
- Schedule được reference từ `QuestCatalogSO` đang preload.
- `contentVersion` hợp lệ và ID đã phát hành không bị trùng.
- Exact target/item ID tồn tại trong `FarmDatabaseSO` hiện tại khi có thể validate chéo.
- Daily v1 không dùng `FarmRipe`/`FarmStageReached`.
- Không có null reference.

Build development phải log lỗi rõ set/quest/objective nào sai. Production không tự chạy với content invalid.

---

## 7. Tích hợp Farm với Quest

### 7.1. Boundary và broker ownership

Quest không thay đổi `FarmModuleInstaller`, `RootLifetimeScope`, Farm payload hoặc `FarmService`.

Quy tắc tích hợp:

- Dùng đúng broker Farm đang tồn tại trong Root/Game composition.
- Không đăng ký lại Farm broker trong Quest installer.
- Không subscribe `FarmSlotChangedPayload` để suy diễn hành động.
- Chỉ `FarmQuestEventBridge` thuộc Quest module được phép hiểu và chuyển đổi Farm payload.
- Duplicate registration hiện có được ghi nhận là technical debt ngoài phạm vi, không phải acceptance criterion của Quest.

### 7.2. Harvest output adapter

`FarmEntityHarvestedPayload` hiện đã có `OutputReward[] Outputs`.

Bridge map mỗi output hợp lệ sang event Quest:

- `TargetId = output.item.ItemId`.
- `Amount = output.amount`.
- Bỏ qua output có `item == null` hoặc `amount <= 0`.
- Một harvested payload luôn tạo tối đa một `FarmHarvestAction`, sau đó tạo một `FarmHarvestItem` cho mỗi output hợp lệ.

Không thêm `FarmHarvestOutput` vào Farm module trong Daily v1.

### 7.3. Progress key khi producer không có EventId

Vì Farm payload hiện không có `EventId`, bridge tạo fingerprint runtime từ:

```text
bridgeSessionId + Time.frameCount + farmEventType + entityId + cell + itemId(optional)
```

Bridge giữ cache fingerprint của frame hiện tại:

- `bridgeSessionId` là GUID tạo một lần khi bridge được construct, tránh collision progress key giữa hai lần mở game.
- Callback trùng cùng payload trong cùng frame chỉ được report một lần.
- Hai hành động hợp lệ ở hai frame khác nhau được tính độc lập.
- Fingerprint sau đó được dùng làm `ProgressKey` cho Quest Core.
- Cache chỉ là runtime dedupe, không persist.
- Không cam kết chống được producer replay cùng hành động ở frame khác; muốn bảo đảm mức đó phải thay Farm contract, hiện ngoài phạm vi.

Failed Farm action hiện không publish domain payload nên không tạo Quest progress.

### 7.4. Farm Quest Event Bridge

`FarmQuestEventBridge` subscribe các payload production:

| Farm payload | Quest event | Amount | Progress key |
|---|---|---:|---|
| Planted | FarmPlanted | 1 | bridge fingerprint |
| Cared | FarmCared | 1 | bridge fingerprint |
| Harvested | FarmHarvestAction | 1 | bridge fingerprint |
| Harvested output hợp lệ | FarmHarvestItem | output amount | bridge fingerprint + itemId |

Mapping target:

- Plant/Care/HarvestAction: `TargetId = payload.EntityId`.
- `TargetCategory = payload.EntityType.ToString()` cho `Crop`/`Animal`.
- HarvestItem: `TargetId = output.item.ItemId`, đồng thời event vẫn mang category của entity nguồn.

Ví dụ:

- “Thu hoạch 10 lần cây trồng”: `ActionCount + FarmHarvestAction + category Crop`.
- “Thu hoạch 10 lúa”: `ItemAmount + FarmHarvestItem + exact target wheat_grain`.
- “Thu hoạch 5 trứng”: `ItemAmount + FarmHarvestItem + exact target egg`.
- “Cho 10 con vật ăn”: `ActionCount + FarmCared + category Animal`.

`FarmQuestTestFlow` không còn tự accept catalog. Thay bằng bridge production; phần debug chỉ được phép tạo quest test khi người dùng chủ động bật.

`FarmRipe` và `FarmStageReached` không được bridge report trong Daily v1 để tránh phụ thuộc thứ tự offline initialization của Farm.

---

## 8. Daily runtime và reset

### 8.1. Daily availability

`DailyAvailabilityState`:

- `WaitingForPlayerData`
- `WaitingForServerTime`
- `Ready`
- `ConfigurationError`

UI khóa interaction nếu state không phải Ready.

### 8.2. Khởi tạo

Daily chỉ initialize khi:

- PlayerData đã load.
- `IServerTimeProvider.IsSynced == true`.
- Schedule hợp lệ.

`DailyQuestBootstrapper` phải xử lý cả hai thứ tự khởi tạo:

1. Khi Game scope start, kiểm tra ngay `IDailyQuestRepository.IsLoaded` và `IServerTimeProvider.IsSynced`.
2. Nếu cả hai đã ready, initialize ngay; không chờ payload đã phát trong Preloading scene.
3. Nếu data chưa ready, await `IDailyQuestRepository.WaitUntilLoadedAsync(ct)`; nếu time chưa ready, subscribe `ServerTimeSyncedPayload`.
4. Mỗi lần server resync vẫn kiểm tra lại day key.
5. `TryInitialize()` idempotent; nhiều tín hiệu ready không được activate quest hai lần.

Flow:

```text
Check repository.IsLoaded hoặc await WaitUntilLoadedAsync
        +
Check time.IsSynced hoặc chờ ServerTimeSyncedPayload
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
Khôi phục pending reward nếu lần trước bị crash
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
2. Deactivate chính xác danh sách RuntimeId của ngày cũ.
3. Bỏ task progress và milestone state cũ.
4. Không tự trao milestone chưa nhận.
5. Chọn set mới.
6. Tạo runtime ID mới.
7. Save state ngày mới ngay lập tức.
8. Mở lại Daily UI và chuyển về page 0.

### 8.5. Progress state

Daily task state lưu:

- Một `QuestRuntimeSnapshot` duy nhất chứa RuntimeId, DefinitionId, status và objective progress.

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

- `GrantCoinsAsync(transactionId, amount, source, cancellationToken)`
- Trả `Granted`, `AlreadyGranted` hoặc `Failed`.

`PlayerData` thêm reward ledger và durable pending-reward outbox.

`PlayerDataHolder`:

- Kiểm tra transaction ID.
- Nếu đã có: trả AlreadyGranted, không cộng coin.
- Nếu chưa có: cộng coin, thêm transaction ID và xóa pending record tương ứng.
- Commit coin + ledger + pending removal bằng immediate atomic write dưới single-writer gate.
- Sau khi save thành công mới publish reward/currency event.
- Nếu save thất bại, không publish popup hoặc báo Granted; reward được đưa vào hàng retry trong phiên hiện tại.

Đổi `SaveImmediate()` trả kết quả thành công/thất bại để caller không đánh dấu reward đã xử lý khi ghi file lỗi.

Single-writer behavior:

- Throttled save và immediate save dùng chung một async lock/gate.
- Trước khi ghi, tạo JSON/snapshot ổn định; không serialize object đang bị gameplay mutate trên thread khác.
- Immediate save cancel pending debounce và chờ writer đang chạy kết thúc trước khi ghi.
- `PlayerDataSaveLoad.Save()` trả `bool` hoặc result có error thay vì nuốt lỗi rồi trả `void`.
- Reward mutation phải idempotent theo transaction ID trong mọi lần retry.
- Nếu commit coin/ledger thất bại, rollback mutation coin/ledger trong RAM nhưng giữ pending record; lần retry sau vẫn đi qua nhánh `Granted` và phát popup đúng một lần.

Retry trong cùng phiên:

- Lần grant đầu tiên chạy ngay khi task hoàn thành hoặc milestone được claim.
- Trước khi grant, task/milestone state và `PendingRewardSaveData` được commit immediate để tạo durable outbox.
- Chỉ gọi `GrantCoinsAsync` sau khi commit durable outbox thành công; nếu bước này lỗi thì retry bước staging trước, không trao coin trên state chưa bền vững.
- Nếu local IO lỗi, `DailyQuestService` giữ reward ở trạng thái Pending và retry bất đồng bộ với backoff 1s, 2s, sau đó tối đa 5s giữa các lần.
- Retry tiếp tục khi game còn chạy và dừng khi transaction trả `Granted`/`AlreadyGranted`.
- Popup chỉ enqueue sau commit thành công.
- Reconcile ở lần mở game sau chỉ là fail-safe nếu app bị kill/crash trước khi retry thành công, không phải normal flow.

### 9.3. Hoàn thành task

```text
QuestCompletedPayload
-> Daily kiểm tra runtime có thuộc ngày hiện tại không
-> Stage task Completed + PendingRewardSaveData
-> Tính lại totalPoints
-> Immediate commit durable pending state
-> Gọi GrantCoinsAsync ngay trong cùng phiên
-> Immediate commit coin + ledger + xóa pending record
-> Nếu Granted: publish popup event
-> Nếu AlreadyGranted: xóa pending nếu còn, persist và không popup lại
-> Nếu Failed: giữ pending trong RAM và retry ngay trong phiên
```

### 9.4. Nhận milestone

```text
Người chơi bấm mốc
-> Kiểm tra Daily Ready
-> Kiểm tra totalPoints >= requiredPoints
-> Kiểm tra milestone chưa Claimed
-> Stage ClaimPending + PendingRewardSaveData và commit immediate
-> Gọi GrantCoinsAsync ngay trong cùng phiên
-> Immediate commit Claimed + coin + ledger + xóa pending record
-> Granted hoặc AlreadyGranted: giữ Claimed và persist
-> Failed: giữ Pending, khóa spam click và retry trong phiên
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

- `QuestRuntimeSnapshot runtime`

`QuestObjectiveProgressSnapshot`:

- `objectiveId`
- `currentAmount`
- `isCompleted`
- `List<string> countedProgressKeys`

`DailyMilestoneSaveData`:

- `requiredPoints`
- `claimed`

`DailyMilestoneStatus` là runtime/UI state `Locked`, `Claimable`, `ClaimPending`, `Claimed`; `Locked/Claimable` được tính lại từ total points, còn `ClaimPending` được suy ra từ pending-reward outbox để tránh persist state dẫn xuất bị stale.

`PlayerData` thêm:

- `DailyQuestSaveData DailyQuest`
- `List<string> GrantedRewardTransactionIds`
- `List<PendingRewardSaveData> PendingRewards`

`PendingRewardSaveData`:

- `transactionId`
- `amount`
- `source`
- `createdDayKey`

Outbox bảo đảm task/milestone đã hoàn thành không thể mất reward nếu app bị kill giữa hai atomic write. Normal flow vẫn grant ngay trong cùng callback/session; startup chỉ xử lý record thật sự còn pending sau crash.

### 10.2. Repository

`IDailyQuestRepository` thuộc Quest assembly:

- `bool IsLoaded`
- `UniTask WaitUntilLoadedAsync(CancellationToken ct)`
- `DailyQuestSaveData Load()`
- `UniTask<SaveResult> SaveAsync(DailyQuestSaveData data, SaveMode mode, CancellationToken ct)`
- `void Clear()`

`PlayerDataHolder` triển khai thêm `IDailyQuestRepository` và `IRewardService`:

- Map repository sang `PlayerDataHolder.Data`.
- Không chứa rule chọn set/reset/reward.
- Cùng reward service tham gia single-writer transaction trên một `PlayerData`.
- Save immediate cho completion + reward, reset và claim + reward.
- Save throttled cho progress tăng nhưng chưa hoàn thành.
- Registration hiện tại đã dùng `.AsImplementedInterfaces()`, vì vậy các interface mới được expose mà không cần sửa `RootLifetimeScope`.

### 10.3. Migration

- Tăng `PlayerData.SaveVersion`.
- Save cũ không có Daily data được xem như chưa khởi tạo Daily.
- Inventory, coin và FarmSlots cũ được giữ nguyên.
- Không cố migrate state từ Quest debug hiện tại vì state đó chưa được lưu.
- Danh sách reward transaction khởi tạo rỗng.
- Pending reward outbox khởi tạo rỗng và mọi list null từ save cũ được normalize trước khi expose Data.

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

Asset strategy:

- `quest hàng ngày 1.png` chỉ là mockup tham chiếu, không dùng nguyên tấm cho Daily runtime.
- `quest hàng ngày_nền 2.png` làm nền chung của Quest window/Daily.
- Tab, gift, coin, lock, progress bar, task icon và reward badge dùng các PNG rời trong `Assets/Module/Quest/Texture`.
- Text tên tab, mô tả task, tiến độ, coin và countdown dùng TMP để cập nhật động.
- `quest tiến độ 3.png` dùng nguyên tấm cho Progress placeholder.
- `quest thực đơn 3.png` dùng nguyên tấm cho Food placeholder.
- Nội dung/nút được vẽ sẵn trong hai placeholder không có hit target và không chạy logic.
- Ba `TabButton` thật là hit target trong suốt đặt đúng lên vùng tab của artwork; khi hiện placeholder không vẽ thêm tab art lần hai.
- Dùng font TMP gần nhất đang có trong project; thay font đúng artwork là polish task về sau.
- Import texture runtime dưới dạng Sprite (2D and UI), giữ alpha và cấu hình compression phù hợp target build.

### 11.2. Phân trang

- `ItemsPerPage = 2`.
- `pageCount = ceil(taskCount / 2)`.
- Page index bắt đầu từ 0 trong code, hiển thị từ 1 trên UI.
- Page đầu disable nút Previous.
- Page cuối disable nút Next.
- Khi reset sang set có ít page hơn, clamp về page hợp lệ; mặc định reset về page đầu.
- Không instantiate lại toàn bộ list mỗi lần đổi page; dùng pool cố định 2 item view và bind lại.

### 11.3. Presenter

`DailyQuestPresenter`:

- Đọc snapshot từ `IDailyQuestService`.
- Tạo view model cho tối đa hai task của page hiện tại.
- Bind point bar, milestone, countdown và button states.
- Subscribe Daily state changed payload.
- Không trao coin, cộng điểm hoặc reset state.

Task reward UI:

- Reward task được tự động trao, không có thao tác claim task.
- Asset “nút nhận thưởng” được dùng như reward badge/trạng thái hiển thị số coin, không phải Button nhận thưởng.
- Khi task chưa complete, badge dùng trạng thái chưa hoàn thành.
- Khi transaction đang retry, view hiển thị trạng thái pending và không phát popup sớm.
- Khi commit thành công, badge chuyển Completed và popup được enqueue.

### 11.4. Reward popup

`DailyRewardPopupQueue`:

- Subscribe reward granted payload.
- Nếu nhiều task hoàn thành từ cùng một event, xếp popup vào queue.
- Popup đầu hiển thị ngay; các popup sau hiển thị tuần tự.
- Popup không điều khiển reward; reward đã commit trước khi popup được enqueue.
- Reward được khôi phục sau crash không phát lại popup; popup chỉ dành cho commit thành công trong phiên hiện tại.

### 11.5. Window lifecycle

`QuestWindowController` kế thừa `WindowController`:

- `IOnBeforeWindowOpen`: bind presenter và luôn mở tab Daily mặc định.
- `IOnWindowClosed`: unbind listener UI.
- Nút close gọi `Close()`.

`QuestWindowLauncher`:

- Nhận Button, `WindowsManager`, `UIWindow`.
- Mở window qua UIManager.
- Lấy instance và inject bằng VContainer theo pattern đang dùng ở `FarmUIBridge`.
- Launcher gắn với một nút Quest trên HUD; nếu scene chưa có nút thì editor setup tạo placeholder button để designer đặt lại vị trí.

Countdown dùng TMP text trong Daily panel:

```text
Làm mới sau 05:32:10
```

Reference layout dùng tỷ lệ artwork 1800x1200 và anchor theo panel để scale cùng Canvas; không hard-code tọa độ màn hình.

### 11.6. Editor setup tool

Tạo mới `QuestUiSetupTool`:

- Tạo đúng hierarchy ba tab và Daily panel.
- Tạo 2 task item slot cố định.
- Tạo 3 milestone view.
- Tạo nút page và label.
- Gắn đúng sprite rời cho Daily và sprite full-panel cho hai placeholder.
- Tạo `UIWindow`/Quest prefab, HUD launcher placeholder và close button dùng icon/button UI gần nhất hiện có.
- Thiết lập Canvas anchors/reference layout theo artwork 1800x1200.
- Không tạo dữ liệu runtime mẫu trong production hierarchy.
- Không ghi đè object có sẵn; báo rõ nếu hierarchy đã tồn tại.

Tool chỉ tự động hóa hierarchy/wiring có thể xác định; designer vẫn có thể tinh chỉnh font, màu, spacing và vị trí HUD trong prefab/Inspector.

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
- `QuestCatalogSO.cs`: giữ quest definitions và reference tới Daily schedule để preload qua Addressables.
- `DailyQuestSaveData.cs`: root save model Daily.
- `IDailyQuestRepository.cs`: persistence abstraction.
- `IDailyQuestService.cs`: API initialize/query/claim/page-independent.
- `DailyQuestService.cs`: lifecycle, reset, restore, completion, points, milestones.
- `DailyQuestScheduleResolver.cs`: tính set index từ server day.
- `DailyQuestValidator.cs`: validation runtime dùng chung.
- `DailyQuestStateChangedPayload.cs`: yêu cầu UI refresh.
- `DailyAvailabilityChangedPayload.cs`: lock/ready/config error.

### Integration

- `FarmQuestEventBridge.cs`: chuyển payload Farm thành QuestProgressEvent.
- `DailyQuestBootstrapper.cs`: check repository/time hiện tại, await repository readiness hoặc chờ server-time event rồi initialize idempotent.

### Reward contract và app data

- `IRewardService.cs`: contract thuộc Quest assembly, grant idempotent bằng transaction ID.
- `RewardGrantResult.cs`: Granted/AlreadyGranted/Failed.
- `RewardGrantedPayload.cs`: Quest-owned broker cho popup và consumer cập nhật hiển thị coin sau commit.
- `PlayerData.cs`: thêm Daily save và reward transaction ledger.
- `PlayerDataHolder.cs`: implement reward service, snapshot và single-writer save gate.
- `PlayerDataSaveLoad.cs`: giữ atomic write, trả success/failure.

### Farm

- Không sửa file Farm trong Daily v1.
- `FarmQuestEventBridge` đọc contract hiện tại: Planted/Cared/Harvested và `OutputReward[]`.
- EventId từ producer và thay đổi broker ownership được để ngoài phạm vi.

### UI

- `QuestWindowController.cs`: lifecycle cửa sổ Quest.
- `QuestWindowLauncher.cs`: mở Quest bằng UIManager và inject instance.
- `DailyQuestPresenter.cs`: state-to-view-model và pagination.
- `DailyQuestTabView.cs`: references UI của tab Daily.
- `DailyQuestItemView.cs`: render một task.
- `DailyMilestoneView.cs`: render/click một milestone.
- `DailyRewardPopupQueue.cs`: hiển thị popup tuần tự.
- `QuestUiSetupTool.cs`: tạo hierarchy static trong Editor.
- `ProgressQuestPlaceholderView.cs`: hiển thị `quest tiến độ 3.png`, không có logic.
- `FoodPlaceholderView.cs`: hiển thị `quest thực đơn 3.png`, không có logic.

### Bootstrap

- `Quest.asmdef`: reference trực tiếp Farm, Loading và Clock/Time; không reference MyOwn.
- `QuestModuleInstaller.RegisterQuestModule()`: Root scope, đăng ký Quest/Daily/Reward-owned brokers và `QuestCatalogProvider`.
- `QuestModuleInstaller.RegisterQuestGameplay()`: Game scope, đăng ký Quest Core, Daily service, bridge và presenter-related services.
- `RootLifetimeScope.cs`: không sửa; registration `PlayerDataHolder.AsImplementedInterfaces()` hiện có tự expose repository/reward interface mới.
- `GameLifetimeScope.cs`: tiếp tục gọi `RegisterQuestGameplay()`; Quest window launcher nằm dưới UI root được auto-inject hoặc được đăng ký scene component.

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

### Giai đoạn 1: Khóa boundary và refactor Quest Core

1. Ghi guardrail: không sửa Root broker registrations hoặc Farm module.
2. Tách event type khỏi objective type.
3. Thêm target matching.
4. Thêm ActionCount và ItemAmount rule.
5. Chuyển runtime identity sang RuntimeId.
6. Thêm snapshot/restore/deactivate.
7. Cập nhật payload Quest mang RuntimeId và DefinitionId.
8. Tạo save DTO/repository/reward contracts thuộc Quest assembly.
9. Cập nhật `Quest.asmdef` để dùng Clock/Time trực tiếp nhưng không tạo reference sang MyOwn.
10. Giữ toàn bộ test cũ dưới behavior tương đương.

### Giai đoạn 2: Persistence và durable reward outbox

1. Mở rộng `PlayerData` với Daily save, reward ledger và pending outbox.
2. Cho `PlayerDataHolder` implement repository/reward interfaces qua registration `.AsImplementedInterfaces()` hiện có.
3. Thêm single-writer gate, stable snapshot và result cho immediate save.
4. Implement rollback in-memory khi reward commit thất bại.
5. Bump save version, migration và regression test Farm save hiện tại.

### Giai đoạn 3: Daily data và Addressables

1. Tạo Entry/Set/Schedule SO.
2. Reference Schedule từ `QuestCatalogSO` đang preload.
3. Tạo resolver vòng lặp theo UTC+7.
4. Tạo validator runtime/editor.
5. Tạo 3 set mẫu, mỗi set 4 task, tổng 100 điểm.
6. Xác nhận dependency content được load qua `QuestCatalogProvider`.

### Giai đoạn 4: Farm bridge không sửa producer

1. Subscribe Planted/Cared/Harvested hiện có.
2. Map action-count và item-amount từ `OutputReward[]`.
3. Thêm fingerprint/cache chống callback trùng cùng frame.
4. Không report Ripe/StageReached trong Daily v1.
5. Bỏ auto-accept khỏi `FarmQuestTestFlow`; debug chỉ chạy khi chủ động bật.

### Giai đoạn 5: Daily application service và reward

1. Implement check-state-or-wait-event cho data/time readiness.
2. Initialize/restore/reset.
3. Activate task tự động.
4. Xử lý completion bằng durable pending outbox rồi grant ngay trong cùng phiên.
5. Tính points từ completed states.
6. Claim milestone bằng transaction idempotent.
7. Retry reward failure ngay trong phiên với backoff.
8. Giữ startup reconcile làm crash fail-safe.

### Giai đoạn 6: UI và texture

1. Tạo Quest window bằng UIManager.
2. Tạo Daily tab động từ nền và asset rời.
3. Tạo pool 2 task item, point bar, milestone và countdown TMP.
4. Implement pagination 2 task/page.
5. Dùng reward asset như badge, không phải task-claim button.
6. Gắn Progress/Food full mockup làm placeholder không tương tác.
7. Tạo HUD launcher, close button và reward popup queue.
8. Tạo mới editor setup tool và dùng font TMP gần nhất.

### Giai đoạn 7: Content và nghiệm thu

1. Validate 3 Daily Set mẫu và toàn bộ target ID.
2. Chạy EditMode tests.
3. Chạy PlayMode integration tests.
4. Test IO failure/retry trong cùng session.
5. Test trên build với WorldTimeAPI thật và trường hợp mất mạng.
6. Visual QA Quest window ở các aspect ratio target.

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

- Repository/time đã ready trước khi Game scope start => initialize ngay.
- Repository/time ready sau khi Game scope start => initialize qua wait/event.
- Ready event lặp => không activate task hai lần.
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
- Completion/claim + pending reward được commit trước, sau đó coin + ledger + pending removal được commit ngay trong cùng session.
- Save lỗi => không popup/không báo Granted và tự retry trong cùng session.
- Retry thành công => coin cập nhật và popup xuất hiện ngay trong session.
- App bị kill trước retry thành công => startup reconcile là fail-safe.
- Pending outbox còn sau crash => grant đúng một lần rồi được xóa.
- AlreadyGranted không hiện popup lại.
- Save failure không báo Claimed giả.
- Throttled save và immediate save không ghi file đồng thời.
- Snapshot đang serialize không bị gameplay mutate từ thread khác.

### Farm bridge

- Plant crop/animal map đúng category.
- Care animal tăng care objective.
- Harvest action tăng 1 dù output amount lớn.
- Harvest item tăng theo amount.
- Multi-output tạo một action event và nhiều item event.
- Output null/amount không hợp lệ bị bỏ qua.
- Callback giống nhau trong cùng frame chỉ tăng một lần.
- Cùng frame/signature ở hai game session khác nhau không collision progress key.
- Cùng hành động signature ở frame khác vẫn được tính độc lập.
- Failed Farm action không tạo quest progress.
- Ripe/StageChanged không tạo Daily progress.

### UI PlayMode

- 1–2 task => một page.
- 3–4 task => hai page.
- 5 task trở lên => số page động.
- Previous/Next disable đúng.
- Reset về page đầu.
- Locked overlay chặn claim/page interaction cần chặn.
- Popup nhiều reward chạy tuần tự.
- Đóng/mở window không đăng ký listener trùng.
- Daily text/progress/reward bind động; không dùng mockup Daily nguyên tấm.
- Progress/Food tab hiển thị đúng full-panel placeholder và không có hit target giả.
- Mở window luôn về Daily; HUD launcher và close button hoạt động.
- Countdown hiển thị và cập nhật đúng.

### Regression

- Farm planting/feeding/harvesting vẫn hoạt động.
- Farm visual vẫn dùng FarmSlotChangedPayload.
- Save cũ load được; Daily/reward/pending lists được normalize đúng.
- Clock/Farm offline progress không bị ảnh hưởng.
- Quest debug tests cũ được cập nhật và vẫn pass.
- Không có source file Farm nào bị sửa.
- Không xóa/thay đổi broker registration hiện có trong Root.

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
- Nếu local save lỗi, reward retry ngay trong phiên và popup chỉ xuất hiện sau commit.
- Milestone claim thủ công, thứ tự tự do và không grant trùng.
- UI hiển thị tối đa 2 task/page, không scroll.
- Daily được ghép động từ asset rời; Progress/Food là placeholder đúng mockup đã chốt.
- Quest không thêm broker registration trùng và không sửa broker/module thuộc dev khác.
- EditMode và PlayMode tests liên quan đều pass.

---

## 17. Phạm vi để sau

Không triển khai trong Daily v1:

- Progress quest chain và Progress milestones.
- Food unlock/catalog runtime.
- Farm Ripe/StageReached objective trong Daily.
- Thêm producer EventId hoặc sửa Farm payload/FarmService.
- Dọn broker registration trùng trong Root/Farm module.
- Random Daily.
- Reroll Daily.
- Cloud save Daily.
- Bù nhiệm vụ cho ngày bỏ lỡ.
- Tự trao milestone chưa nhận.
- Notification ngoài popup reward.

Quest Core, reward ledger và persistence contracts được chuẩn bị để các giai đoạn này dùng lại.
