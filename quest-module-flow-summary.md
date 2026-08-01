# Quest module — flow code hiện tại

## 1. Phạm vi đã triển khai

- Daily Quest hoạt động đầy đủ với 4 task/ngày, 2 task/trang.
- Có 3 bộ Daily Quest, xoay vòng theo ngày UTC+7.
- Mỗi task hoàn thành cho 25 điểm và tự động cộng 100 coin.
- Ba milestone ở 25/50/100 điểm, người chơi bấm nhận lần lượt 100/300/500 coin.
- Tab `Tiến độ` và `Thực đơn` đã ghép bằng full mockup PNG, chỉ chuyển tab và chưa có logic.
- Daily bị khóa cho tới khi `IServerTimeProvider.IsSynced == true`.
- Progress Daily lưu local trong `PlayerData`; Firebase chưa tham gia.
- Không sửa Farm producer/payload/installer và không xóa các broker trùng trong Root.

## 2. Boot và dependency flow

```text
Preloading
  -> QuestCatalogProvider load Addressable "QuestCatalog"
  -> MapSceneBootstrap enqueue QuestCatalogSO vào Game scope
  -> GameLifetimeScope.RegisterQuestGameplay()
       -> QuestService + objective rules
       -> DailyQuestService + DailyQuestBootstrapper
       -> FarmQuestEventBridge

Root scope
  -> PlayerDataHolder đã tồn tại
  -> AsImplementedInterfaces() tự expose:
       IDailyQuestRepository
       IQuestRewardService
```

`DailyQuestBootstrapper` chỉ khởi động async initialization và không chặn game load. Nếu WorldTimeAPI chưa sync, Daily vẫn ở trạng thái locked nhưng gameplay khác tiếp tục chạy.

## 3. Daily initialization

`DailyQuestService.EnsureInitializedAsync()` thực hiện theo thứ tự:

1. Chờ `PlayerDataHolder` load xong.
2. Chờ server time sync.
3. Lấy `DailyQuestScheduleSO` từ `QuestCatalogSO.dailySchedule`.
4. Tính `dayKey` theo UTC+7, dạng `yyyy-MM-dd`.
5. Đọc `DailyQuestSaveData` local:
   - cùng ngày và set còn hợp lệ: restore;
   - khác ngày, mất set hoặc chưa có save: chọn set mới theo ngày và tạo runtime mới.
6. Gọi `QuestService.ActivateQuest(runtimeId, definitionId, snapshot)` cho từng task.
7. Reconcile reward transaction còn pending do crash/save lỗi ở phiên trước.

Runtime ID có dạng:

```text
daily:{dayKey}:{index}:{questDefinitionId}
```

Definition ID và runtime ID tách riêng, nên cùng một definition có thể tái sử dụng ở ngày khác mà không ăn nhầm progress cũ.

## 4. Farm event sang Quest event

`FarmQuestEventBridge` chỉ subscribe các public payload hiện có:

```text
FarmEntityPlantedPayload
  -> QuestEventType.FarmPlanted

FarmEntityCaredPayload
  -> QuestEventType.FarmCared

FarmEntityHarvestedPayload
  -> QuestEventType.FarmHarvestAction (1 lần thao tác)
  -> QuestEventType.FarmHarvestItem (amount theo từng output item)
```

Harvest outputs cùng `ItemId` được gộp amount trước khi report.

Do Farm payload hiện không có `EventId`, bridge tạo progress key:

```text
bridgeSessionId + frameCount + eventType + entity/cell + itemId
```

Key này chống việc cùng một callback bị report hai lần trong cùng frame. Session GUID tránh collision giữa hai lần chạy game. Đây là adapter-side dedupe; nếu Farm sau này có `EventId` thật thì nên thay key tạm này bằng producer event ID.

## 5. Quest Core xử lý progress

`QuestService.ReportEvent()` duyệt các runtime đang active:

1. Lấy definition bằng `QuestDefinitionId`.
2. Với từng objective, lấy rule theo `QuestObjectiveType`.
3. Rule kiểm tra `QuestEventType` và target:
   - `Any`;
   - `ExactTarget`;
   - `TargetCategory`.
4. `QuestProgressApplier` kiểm tra progress key, cộng amount và cap ở required amount.
5. Publish `QuestProgressChangedPayload`.
6. Khi mọi objective hoàn tất:
   - runtime chuyển `Completed`;
   - bị loại khỏi active list;
   - publish `QuestCompletedPayload` đúng một lần.

Ba rule hiện có:

- `StateReachedObjectiveRule`: giữ compatibility cho quest cũ.
- `ActionCountObjectiveRule`: mỗi event hợp lệ chỉ cộng 1, không tin amount của event.
- `ItemAmountObjectiveRule`: cộng đúng số lượng item harvest.

Daily content v1 chỉ dùng ActionCount và ItemAmount; không dùng Ripe/Stage.

## 6. Save progress

Khi nhận `QuestProgressChangedPayload`, `DailyQuestService`:

1. Lấy snapshot mới từ `QuestService`.
2. Gắn snapshot vào task save.
3. Gọi repository save throttled 1 giây.
4. Publish `DailyQuestStateChangedPayload` để UI render lại.

`PlayerDataHolder` vẫn là owner duy nhất của file `playerdata.json`.

`PlayerDataSaveLoad` dùng:

- JSON snapshot ổn định được tạo trên main thread;
- IO chạy thread pool cho save throttled;
- lock chung cho mọi writer;
- monotonic save revision để một throttled writer cũ không thể overwrite một immediate save mới;
- temp file rồi rename.

## 7. Task reward tự động

Khi `QuestCompletedPayload` tới:

1. Daily đánh dấu task `rewardQueued`.
2. Tạo transaction ID:

   ```text
   daily-reward:{dayKey}:task:{runtimeId}
   ```

3. `StageQuestRewardAsync()` ghi atomically trong cùng `PlayerData`:
   - Daily snapshot đã completed;
   - pending reward transaction.
4. Ngay trong cùng session, `GrantPendingCoinsAsync()`:
   - kiểm tra granted ledger;
   - cộng coin nếu transaction chưa grant;
   - thêm transaction vào ledger;
   - xóa pending;
   - save immediate.
5. Publish `QuestRewardGrantedPayload`; UI hiện `+coin`.

Nếu stage hoặc grant save thất bại, Daily retry sau 1s, 2s, rồi mỗi 5s cho tới khi thành công hoặc scope bị dispose. Vì transaction ID và granted ledger cố định, retry không cộng trùng coin.

Startup reconcile cũng grant pending reward, nhưng payload có `ReconciledAtStartup = true` nên UI không bật popup cũ.

## 8. Milestone reward thủ công

UI lấy `DailyMilestoneClaimState`:

- `Locked`: chưa đủ điểm.
- `Claimable`: đủ điểm, cho bấm.
- `ClaimPending`: đã stage nhưng coin đang retry.
- `Claimed`: đã grant xong.

Khi bấm:

1. Daily kiểm tra điểm và claimed state.
2. Đánh dấu claimed trong daily state.
3. Stage transaction:

   ```text
   daily-reward:{dayKey}:milestone:{milestoneId}
   ```

4. Grant coin ngay; nếu lỗi thì chuyển `ClaimPending` và retry cùng session.

## 9. Reset ngày

Mỗi `ClockTickPayload`, Daily:

- cập nhật countdown cho UI;
- so sánh day key hiện tại với save.

Khi đổi ngày:

1. Deactivate toàn bộ runtime ID của ngày cũ.
2. Chọn một trong 3 set theo day number modulo số set.
3. Tạo 4 runtime mới.
4. Save immediate state ngày mới.
5. Activate runtime mới và refresh UI.

Remote hot-swap/delete set giữa ngày không được hỗ trợ. Nếu save tham chiếu set không còn tồn tại, lần initialize tiếp theo sẽ tạo ngày mới từ schedule hiện tại.

## 10. UI flow

`QuestProjectSetup` tự động/idempotent:

- đổi PNG trong `Assets/Module/Quest/Texture` sang Sprite;
- tạo 12 definitions, 3 sets và một schedule;
- nối schedule vào Addressable `QuestCatalogSO`;
- dựng `QuestWindow.prefab`;
- tạo `PrefabUIWindow` tên `QuestWindow`;
- đăng ký window vào `UIWindowCollection` ở Popup layer.

Có thể rebuild bằng menu:

```text
Tools > Quest > Rebuild Quest Content & UI
```

`QuestUIBootstrap` dùng runtime initialization, chờ Game scope và `WindowsManager`, sau đó tự tạo nút `NHIỆM VỤ` ở HUD. Cách này không sửa scene hierarchy của module khác.

Khi mở:

1. `QuestHudLauncher` gọi `WindowsManager.Open(QuestWindow)`.
2. Resolve và inject `QuestWindowController`.
3. Window mặc định ở tab Daily, page 1.
4. `QuestWindowController` render đúng 2 task/page.
5. Previous/Next đổi giữa hai trang.
6. Daily state, countdown và reward payload làm UI refresh.
7. Tab Progress/Food chỉ bật full mockup placeholder tương ứng.

Font dùng `LiberationSans SDF`; fallback dynamic hiện có xử lý glyph ngoài atlas chính.

## 11. Content hiện tại

Mỗi set có 4 task:

- Set 1: gieo wheat, chăm animal, harvest action, lấy `wheat_grain`.
- Set 2: gieo sugarcane, chăm crop, harvest animal, lấy `egg`.
- Set 3: gieo crop bất kỳ, chăm animal, harvest sugarcane, lấy `sugarcane_raw`.

Mỗi task: 25 điểm, 100 coin.

## 12. Debug cũ và boundary

- `FarmQuestTestFlow`, `QuestTestPanelBootstrap` và `QuestTestPanelView` chỉ compile khi bật define `QUEST_DEBUG_FLOW`.
- Production không còn auto-accept toàn bộ catalog và không còn debug panel.
- Không có thay đổi trong Farm module.
- Không xóa/đổi broker registration ở `RootLifetimeScope`.

## 13. Điểm cần nhớ khi debug

- Daily không hiện: kiểm tra server time đã sync và `QuestCatalog.dailySchedule` không null.
- Farm làm nhưng quest không tăng: kiểm tra target ID/category trong definition và payload type bridge nhận được.
- Coin chưa cộng: xem pending transaction trong `PlayerData.PendingQuestRewards`; service sẽ retry cùng session.
- Coin bị nghi cộng hai lần: kiểm tra `GrantedQuestRewardTransactions` theo transaction ID.
- UI không mở: kiểm tra `QuestWindow` có trong `UIWindowCollection` và `WindowsManager` tồn tại ở gameplay scene.
