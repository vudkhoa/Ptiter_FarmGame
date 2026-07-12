# Firebase System — Flow

## Sơ đồ tổng thể

```
[Container build]              [Gate — 1 lần]                  [Consumer — chờ ready]           [Firebase]
RootLifetimeScope       ──tạo──> FirebaseInitService    ──event──> FirebaseCloudService   ──gọi──> Auth + Firestore
  RegisterMessageBroker           StartAsync():                     StartAsync():
    <FirebaseReadyPayload>          CheckAndFixDependencies           if gate.isReady → chạy luôn
  Register 2 service                == Available ?                    else subscribe(ReadyPayload)
                                     → isReady = true                        │
                                     → Publish FirebaseReadyPayload ─────────┘  (callback khi ready)
                                                                        AcquireIdAsync():
                                                                          PlayerId rỗng?
                                                                            → SignInAnonymously → UID
                                                                            → PlayerData.PlayerId = UID
                                                                          SetAsync player_data/{PlayerId}
                                                                            { playerId, time }
```

**Cổng chặn:** không ai gọi Firebase trước khi `FirebaseInitService` xong (`CheckAndFixDependencies == Available`). Consumer biết qua `IFirebaseGate.isReady` **hoặc** event `FirebaseReadyPayload` → pattern "ready-hoặc-chờ" phủ cả 2 thứ tự khởi tạo, không kẹt.

**Kết thúc:** `AcquireIdAsync` chạy **1 lần** khi ready. Có `PlayerId` rồi → **bỏ qua sign-in**, chỉ ghi Firestore (`SetAsync` idempotent nhờ `MergeAll`).

> ⚠️ Hiện `_holder.SaveImmediate()` đang **comment** ("Waiting Merge code") → UID chưa persist xuống save local. Lần mở sau `PlayerId` lại rỗng → sign-in lại (Firebase trả **cùng UID**) → set lại. Bật `SaveImmediate` khi muốn cache bền.

## Các file (`Assets/myOwn/Scripts/Firebase/`)

| File | Vai trò |
|------|---------|
| `FirebaseReadyPayload.cs` | event (struct rỗng) báo Firebase sẵn sàng |
| `IFirebaseGate.cs` | cờ `isReady` cho consumer đọc |
| `FirebaseInitService.cs` | **cổng**: `CheckAndFixDependencies` → `isReady` + `Publish` |
| `FirebaseCloudService.cs` | lấy UID (anonymous auth) + ghi `player_data/{PlayerId}` lên Firestore |

Đăng ký DI: `Assets/myOwn/Scripts/Bootstrap/RootLifetimeScope.cs` → `#region Firebase Block` (broker + 2 service, `.AsImplementedInterfaces()`).

## Chỗ config / chỉnh

- **Collection Firestore:** hằng `COLLECTION = "player_data"` trong `FirebaseCloudService.cs`.
- **Firebase project config:** `Assets/google-services.json` + `Assets/GoogleService-Info.plist`.
- **Firestore instance:** `FirebaseFirestore.DefaultInstance` (database `(default)`; region set 1 lần lúc tạo, KHÔNG truyền vào code).
- **Console bắt buộc bật:** Authentication ▸ Sign-in method ▸ **Anonymous = Enabled**; Firestore ▸ Rules ▸ **test mode** (`allow read, write: if true`).

## Vai trò từng interface (VContainer)

- `IAsyncStartable` → VContainer **tự gọi `StartAsync`** lúc container build (cả 2 service dùng).
- `IFirebaseGate` → để `FirebaseCloudService` inject + đọc `isReady` (chính là `FirebaseInitService` singleton).
- `IDisposable` (FirebaseCloudService) → VContainer tự gọi `Dispose` khi container hủy → gỡ subscription.
- `IPublisher` / `ISubscriber` (MessagePipe) → kênh `FirebaseReadyPayload`, cần `RegisterMessageBroker` mới resolve được.
