# Firebase Setup cho Project — Guide (Ptiter_FarmGame)

> **Mục đích:** checklist + sơ đồ + code mẫu để gắn Firebase vào project này. Setup/gotcha đúc từ project thật **GaLac**; init flow **viết theo kiến trúc Ptiter_FarmGame** (VContainer DI + MessagePipe). Lý thuyết sâu (SDK vỏ C#→native, EDM4U, threading) xem `UnityGameDev_Common/.../SDK/Firebase.md`.

**Model hiện tại (giai đoạn 1):** user vào game → nếu `PlayerData.PlayerId` rỗng thì **đăng nhập ẩn danh (Anonymous Auth) lấy Firebase UID** → lưu vào `PlayerData`; đã có id rồi thì thôi. ID do **Firebase cấp + tự persist** (bền hơn GUID tự sinh, nâng lên login thật giữ nguyên ID). Products cần: **Auth + Firestore + Analytics + Crashlytics + RemoteConfig**.

> ℹ️ **Vì sao Anonymous Auth chứ không GUID tự sinh:** UID ẩn danh do Firebase quản → (1) tự persist qua các lần mở app, (2) **link email/Google sau vẫn giữ nguyên UID** (không mất data), (3) Security Rules siết được theo `request.auth.uid`. "Ẩn danh" = không có màn login, user không thấy gì. Base: lấy + cache UID rồi ghi doc `player_data/{PlayerId}` lên Firestore.

---

## 0. Sơ đồ tổng — từ web tới code

```
┌─ A. SETUP (làm 1 lần, ngoài code) ─────────────────────────────┐
│ Firebase Console                                                │
│   1. Tạo project + bật Analytics (chọn "Default Account")       │
│   2. Add app "Unity" → package name = com.ptitgame.farm         │
│        (PHẢI khớp Player Settings, KHÔNG đổi được sau đăng ký)   │
│   3. Tải google-services.json + GoogleService-Info.plist        │
│   4. Firestore Database → Create → TEST MODE (rules mở, §7)      │
│            │                                                     │
│            ▼                                                     │
│ Unity                                                           │
│   5. Bỏ 2 file config vào  Assets/                              │
│   6. Import .unitypackage: Firestore, Analytics, Crashlytics,   │
│        Auth, RemoteConfig                                      │
│        (lõi FirebaseApp nhúng SẴN trong mỗi package)            │
│   7. Assets ▸ EDM ▸ Android Resolver ▸ Force Resolve (kéo .aar) │
└─────────────────────────────────────────────────────────────────┘
            │
            ▼
┌─ B. GIT LFS (binary native >100MB, GitHub chặn) ───────────────┐
│   8. .gitattributes: track *.bundle *.dll *.so *.aar ...        │
│   9. git lfs install                                            │
│  10. Commit 1: .gitattributes            (luật LFS vào TRƯỚC)   │
│  11. Commit 2: Assets/Firebase/** + config files               │
│  12. git lfs ls-files (verify) → push                          │
│   ⚠ MỌI máy team: `git lfs install` 1 lần TRƯỚC khi clone/pull  │
└─────────────────────────────────────────────────────────────────┘
            │
            ▼
┌─ C. CODE (init flow — VContainer + MessagePipe) ───────────────┐
│  RootLifetimeScope:                                             │
│    RegisterMessageBroker<FirebaseReadyPayload>                  │
│    Register FirebaseInitService + FirebaseCloudService          │
│            │                                                     │
│  FirebaseInitService (IAsyncStartable) — CỔNG duy nhất          │
│    └ CheckAndFixDependenciesAsync == Available                  │
│         → isReady = true → Publish FirebaseReadyPayload          │
│            │                                                     │
│  FirebaseCloudService                                          │
│    └ if gate.isReady → init ; else subscribe ReadyPayload       │
│         → AcquireId (Firebase UID) → Firestore player_data/{PlayerId} │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Setup 1 lần (Console → Unity)

| # | Bước | Lưu ý chết người |
|---|------|------------------|
| 1 | Tạo project [Firebase Console](https://console.firebase.google.com) + bật Analytics | chọn **"Default Account for Firebase"** (free) |
| 2 | Add app **Unity** (không phải Android riêng) → tick Android + iOS | Unity option tải được **cả 2** file config |
| 3 | **Package name / Bundle ID** = `com.ptitgame.farm` | **PHẢI khớp** Player Settings; phân biệt hoa/thường; **không đổi được sau đăng ký** |
| 4 | **Firestore Database ▸ Create database ▸ Start in test mode** | test mode = rules mở (ai cũng đọc/ghi) — đủ cho giai đoạn không-auth; **khóa lại trước release** (§7) |
| 5 | Tải config → **`Assets/`** | `google-services.json` + `GoogleService-Info.plist`. Commit vào repo — **không phải secret** |
| 6 | Import `.unitypackage`: **Auth, Firestore, Analytics, Crashlytics, RemoteConfig** | Auth để lấy UID ẩn danh; lõi App nhúng sẵn; chỉ import cái thật dùng |
| 7 | **EDM ▸ Android Resolver ▸ Force Resolve** | quên = thiếu `.aar` = **crash runtime** dù editor im |

> **Free tier (Spark):** đủ cho Firestore + Analytics + Crashlytics + RemoteConfig. Firestore free ~1 GiB lưu, 50K đọc + 20K ghi/ngày — dư cho dev. Chỉ **Functions** mới cần Blaze → đừng import.
> **Không cần SHA-1/SHA-256** vì chưa dùng Auth (SHA chỉ cho Google/Phone Sign-In).

---

## 2. Git LFS — commit binary Firebase cho team

Binary native Firebase (vd `FirebaseCppApp-*.bundle`) **>100MB** → GitHub **chặn push vĩnh viễn** nếu commit thẳng. Team **bắt buộc dùng Git LFS**.

### 2.1 `.gitattributes` (root repo)

```gitattributes
# Firebase / native binaries qua Git LFS (thường >100MB)
*.bundle  filter=lfs diff=lfs merge=lfs -text
*.dll     filter=lfs diff=lfs merge=lfs -text
*.so      filter=lfs diff=lfs merge=lfs -text
*.a       filter=lfs diff=lfs merge=lfs -text
*.dylib   filter=lfs diff=lfs merge=lfs -text
*.aar     filter=lfs diff=lfs merge=lfs -text
*.srcaar  filter=lfs diff=lfs merge=lfs -text
*.jar     filter=lfs diff=lfs merge=lfs -text
*.framework/** filter=lfs diff=lfs merge=lfs -text

* text=auto
```
> Track theo **đuôi file** (không theo path) để `.meta` (text, cần diff) **không** bị đẩy vào LFS.

### 2.2 Lệnh (đã chạy trong project này)

```bash
git lfs install                 # cài hook LFS cho repo (mỗi máy 1 lần)
git lfs track                   # xem patterns đang track
git check-attr filter -- "Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_13_0.bundle"
                                # → phải ra "filter: lfs"
```

### 2.3 Commit đúng thứ tự — **2 commit**

```
Commit 1  →  .gitattributes                       (luật LFS vào TRƯỚC)
Commit 2  →  Assets/Firebase/**
             Assets/ExternalDependencyManager/**
             Assets/GeneratedLocalRepo/**
             Assets/Plugins/Android|iOS|tvOS/**
             Assets/google-services.json + GoogleService-Info.plist
```
Sau Commit 2, **verify trước khi push**:
```bash
git lfs ls-files      # phải liệt kê FirebaseCppApp-*.bundle, *.dll... → OK mới push
```

### 2.4 ⚠️ Luật onboarding cho MỌI thành viên (ghi vào README)

```
Trước khi clone/pull lần đầu:   git lfs install
Đã lỡ clone trước khi cài LFS:  git lfs install  &&  git lfs pull
```
> Không cài LFS → pull về nhận **file pointer text** thay vì binary → Firebase gãy. Gotcha team duy nhất, phải phổ biến. Quota LFS free GitHub: **1 GB lưu + 1 GB bandwidth/tháng**.

---

## 3. Init flow — nguyên tắc

**Quy tắc vàng:** KHÔNG gọi API Firebase (kể cả Firestore) trước khi qua cổng `CheckAndFixDependenciesAsync == Available`.

Ptiter_FarmGame dùng **VContainer + MessagePipe**. Cổng làm bằng **1 service `IAsyncStartable`** (tự chạy lúc container build), xong thì **publish payload** + phơi cờ `isReady` qua interface. Consumer dùng pattern **"ready-hoặc-chờ"** để không lệ thuộc thứ tự khởi tạo.

```
FirebaseInitService.StartAsync  ──await CheckAndFix──►  Available?
   │                                                      │
   │  (đang await, yield → consumer kịp subscribe)         ├─ không → LogError, DỪNG
   ▼                                                       └─ có → isReady=true → Publish
Consumer.StartAsync
   if gate.isReady → init luôn        (ca: cổng xong trước)
   else subscribe ReadyPayload        (ca: consumer chạy trước)
```

---

## 4. PlayerData — field `PlayerId`

> **📁 File có sẵn:** `Assets/myOwn/Scripts/Data/PlayerData.cs`

`PlayerId` (đã có sẵn) dùng làm **Firestore doc id**. PlayerData hiện tại:

```csharp
[Serializable]
public class PlayerData
{
    public string PlayerId;        // id định danh (Firebase UID). Rỗng = chưa lấy.
    public int SaveVersion = 1;
    public long LastSaveUtcTicks;
    // TODO: thêm game-specific fields (Currency, Inventory, Quests, ...).
}
```
> `PlayerId` rỗng lần đầu → `FirebaseCloudService` lấy Firebase UID gán vào (§5.1). Lần sau đọc lại đúng id → trỏ đúng doc.

---

## 5. Scripts init flow (viết theo VContainer — bạn tự tạo file)

> Namespace `myOwn.Firebase`. Dùng `using MyOwn.ServiceHarness;` để truy cập `PlayerData` / `PlayerDataHolder` / `IService` (nằm ở `MyOwn.ServiceHarness`).
>
> **📁 Vị trí lưu:** tất cả script Firebase đặt trong **`Assets/myOwn/Scripts/Firebase/`** (tạo folder mới). Lý do ở `myOwn` chứ không phải `Module/`: chúng phụ thuộc `PlayerData` + `PlayerDataHolder` (nằm ở `myOwn`) và được wiring ở `RootLifetimeScope` — nếu để trong `Module/` sẽ đảo chiều phụ thuộc (`Module → MyOwn`), phá quy tắc "module không biết app" và sai `asmdef`. Mỗi block dưới ghi rõ **file mới** hay **sửa file có sẵn**.
>
> **⚙️ asmdef:** folder `Firebase/` nằm trong `Assets/myOwn/Scripts/` nên **tự thuộc assembly `MyOwn`** (không cần tạo asmdef mới). Firebase SDK DLL mặc định **Auto Referenced** + `MyOwn.asmdef` có `overrideReferences: false` → assembly `MyOwn` **tự thấy** `Firebase.App/Firestore/...`, khỏi thêm reference. Nếu build báo lỗi "type Firebase.* not found": mở 1 DLL Firebase trong `Assets/Firebase/Plugins/`, Inspector tick **Auto Reference**.

### 5.1 Các file (đọc code thật — doc KHÔNG chép lại)

> **📁 Tạo mới trong** `Assets/myOwn/Scripts/Firebase/` — namespace `myOwn.Firebase`, `using MyOwn.ServiceHarness;` để thấy `IService` / `PlayerData` / `PlayerDataHolder`.

| File | Vai trò | Interface |
|------|---------|-----------|
| `FirebaseReadyPayload.cs` | event (struct rỗng) báo Firebase sẵn sàng | — |
| `IFirebaseGate.cs` | cờ `isReady` cho consumer đọc | — |
| `FirebaseInitService.cs` | **cổng**: `CheckAndFixDependencies` → `isReady` + `Publish(FirebaseReadyPayload)` | `IAsyncStartable, IFirebaseGate, IService` |
| `FirebaseCloudService.cs` | chờ ready → lấy UID (anonymous auth) → ghi `player_data/{PlayerId}` lên Firestore | `IAsyncStartable, IService, IDisposable` |

→ **Source of truth = file `.cs`** (mở đọc trực tiếp, đừng chép vào doc để khỏi lệch). Luồng chạy tổng thể: xem [`FIREBASE_FLOW.md`](FIREBASE_FLOW.md).

### 5.2 Đăng ký trong `RootLifetimeScope.Configure()`

> **📁 SỬA file có sẵn:** `Assets/myOwn/Scripts/Bootstrap/RootLifetimeScope.cs`

```csharp
// --- Firebase Block ---
builder.RegisterMessageBroker<FirebaseReadyPayload>(options);

builder.Register<FirebaseInitService>(Lifetime.Singleton)
    .AsImplementedInterfaces()   // IService, IAsyncStartable, IFirebaseGate
    .AsSelf();

builder.Register<FirebaseCloudService>(Lifetime.Singleton)
    .AsImplementedInterfaces()
    .AsSelf();
```

### 5.3 Vì sao pattern an toàn (không lệ thuộc thứ tự)

- `FirebaseInitService.StartAsync` **`await`** CheckAndFix → **yield** → consumer `StartAsync` **kịp subscribe** trước khi publish → ca "consumer chạy sau" nhận được event.
- Consumer check `_gate.isReady` trước → cổng đã xong thì init luôn → ca "consumer chạy trước" cũng OK.
- Hai nhánh phủ cả 2 thứ tự → **không kẹt vĩnh viễn**, không race.

---

## 6. Luồng dữ liệu — lấy UID + ghi Firestore

```
User mở game
   → FirebaseInitService: CheckAndFix Available → Publish ready
   → FirebaseCloudService.AcquireIdAsync:
        PlayerData.PlayerId rỗng?  → SignInAnonymouslyAsync (Firebase cấp UID)
                                   → data.PlayerId = auth.CurrentUser.UserId
        (đã có id → dùng lại, bỏ qua sign-in)
   → Ghi Firestore: player_data/{PlayerId} = { playerId, time }   (SetAsync MergeAll)
```
Kết quả: `PlayerData.PlayerId` = UID Firebase (lấy 1 lần); 1 doc `player_data/{PlayerId}` trên Firestore.

> ⚠️ Hiện `_holder.SaveImmediate()` đang **comment** → UID chưa persist local → lần sau `PlayerId` lại rỗng → sign-in lại (Firebase trả **cùng UID**). Bật `SaveImmediate` để cache bền.

---

## 7. Firestore Security Rules

Giai đoạn không-auth **phải để rules mở** thì client mới ghi được (không có `request.auth`). Console tạo test mode sẵn:

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /player_data/{playerId} {
      allow read, write: if true;   // ⚠️ TEST MODE — ai biết id đều đọc/ghi được
    }
  }
}
```

> 🔴 **Đây là rules KHÔNG an toàn** — bất kỳ ai (biết GUID) đều đọc/ghi được doc. Chấp nhận cho dev vì GUID khó đoán, nhưng:
> - **Bật App Check** để chặn request không đến từ app thật (giảm abuse dù rules mở).
> - **Trước release: siết rules theo uid** `allow read, write: if request.auth != null && request.auth.uid == playerId;`. Đây là lớp chống cheat backend #1 — script client không thay được.

---

## 8. Gotchas (nhớ khi code)

**Áp dụng ngay (giai đoạn không-auth):**
- **`Crashlytics.ReportUncaughtExceptionsAsFatal = true`** → exception không bắt = **fatal crash**. Save lúc thoát **phải try/catch**, không 1 IOException lúc tắt máy bị báo crash oan.
- **`OnApplicationPause` save phải off main thread** — ghi disk trên main thread lúc OS pause gây **ANR** máy yếu (async khi pause; sync + try/catch chỉ khi quit).
- **Chờ Firebase phải có timeout** ở màn loading (`CancellationTokenSource`) — mạng lỗi thì không treo game, vẫn chơi offline.
- **Đừng block main thread** bằng `.Result`/`.Wait()` → đơ. Luôn `await` (UniTask).
- **Quên Force Resolve EDM** → thiếu `.aar` → crash runtime dù editor im.
- **Package name lệch** Console↔Player Settings → init fail, không sửa được sau đăng ký.

**iOS (vì đang dùng Auth + ghi Firestore):**
- **token race với Firestore = native abort (KHÔNG catch từ C#)** → nếu gặp, thêm `WaitForAuthTokenReadyAsync` (ép lấy token) trước khi chạm Firestore.
- **obfuscation:** class đụng Firebase gắn `[DoNotObfuscateClass, DoNotObfuscateMethodBody]`.

---

*Nguồn tham chiếu: `GaLac/Assets/Scripts/Preloading/BootstrapServices.cs`, `GaLac/Assets/Scripts/PlayerData/FirebaseService.cs`. Lý thuyết: `UnityGameDev_Common/.../SDK/Firebase.md`. Init flow §5 viết theo VContainer của Ptiter_FarmGame. Model: Anonymous Auth UID + ghi Firestore.*
