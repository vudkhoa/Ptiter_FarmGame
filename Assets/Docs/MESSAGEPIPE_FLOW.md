# MessagePipe — Hộp thư sự kiện & Module Installer

> Giải thích kiểu "nói cho dễ hiểu" về `{Module}ModuleInstaller.cs`.
> Phần vòng đời VContainer tổng quát: xem `VCONTAINER_SEQUENCE.md`.

---

## 1. Ví von nền: cái bưu điện

| Code | Đời thường |
|---|---|
| `RegisterMessagePipe()` | **Xây bưu điện** (hạ tầng chung, làm 1 lần duy nhất) |
| `RegisterMessageBroker<T>()` | **Mở 1 hộp thư riêng** cho MỘT loại thư |
| `IPublisher<T>` | Quyền **bỏ thư** vào hộp |
| `ISubscriber<T>` | Đăng ký **nhận thư** từ hộp |
| `MessagePipeOptions` | **Bản nội quy** chung của bưu điện |

Mỗi loại payload có hộp thư riêng, không lẫn nhau.
**Quên mở hộp → ai xin quyền gửi/nhận sẽ bị văng lỗi ngay lúc khởi động.**

---

## 2. Bóc từng dòng khó hiểu

```csharp
using MessagePipe;
using VContainer;

namespace Core.Module.Input
{
    public static class InputModuleInstaller
    {
        public static IContainerBuilder RegisterInputEvents(
            this IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<PointerScreenPayload>(options);
            builder.RegisterMessageBroker<PointerButtonDownPayload>(options);
            builder.RegisterMessageBroker<KeyDownPayload>(options);
            return builder;
        }
    }
}
```

### `this IContainerBuilder builder`  ← chỗ ảo diệu nhất
Chữ `this` biến hàm thường thành **extension method**. Nó cho phép viết:

```csharp
builder.RegisterInputEvents(options);                          // viết thế này
InputModuleInstaller.RegisterInputEvents(builder, options);    // thực chất là thế này
```

Hai dòng **giống hệt nhau** — compiler tự dịch. Chỉ là đường cú pháp cho đẹp, **không tốn gì lúc chạy**.
Ý nghĩa: `IContainerBuilder` là interface của VContainer, ta không sửa được nó, nhưng `this` cho phép "gắn thêm" hàm vào nó từ bên ngoài.

### `IContainerBuilder` — bản chất
**Tờ đơn đặt hàng** điền lúc `Configure`. **KHÔNG phải** container. Ruột nó chỉ là 1 cái `List`:

```csharp
readonly List<RegistrationBuilder> registrationBuilders = new List<RegistrationBuilder>();

public T Register<T>(T registrationBuilder)
{
    registrationBuilders.Add(registrationBuilder);   // ← chỉ có thế
    return registrationBuilder;
}
```

Toàn bộ interface **chỉ có 3 hàm + 4 property**:

| Thành viên | Việc |
|---|---|
| `Register<T>(T)` | Thêm 1 công thức vào List — **hàm gốc duy nhất** |
| `RegisterBuildCallback(Action)` | Chạy hàm này sau khi build xong |
| `Exists(Type, ...)` | Đã đăng ký chưa? |
| `Count`, `this[int]`, `ApplicationOrigin`, `Diagnostics` | Phụ trợ / chẩn đoán |

Mọi thứ khác — `Register<T>(Lifetime.Singleton)`, `RegisterInstance`, `RegisterComponentInHierarchy`, `RegisterEntryPoint`, `RegisterMessageBroker`, và `RegisterInputEvents` của bạn — **đều là extension method** dựng trên đúng 1 hàm `Register` đó.

**Builder ≠ Container** — hai object khác nhau:

| | `IContainerBuilder` | `IObjectResolver` (Container) |
|---|---|---|
| Là gì | Tờ đơn đặt hàng | Cái bếp đã dựng xong |
| Sống khi nào | Chỉ trong `Configure` | Suốt đời game |
| Làm gì | `Register(...)` ghi công thức | `Resolve(...)` đẻ object thật |
| Sau `Build()` | **Vứt đi** | Bắt đầu chạy |

### `MessagePipeOptions options`
**Bản nội quy chung**, tạo đúng 1 lần ở Root. Bên trong có 5 nút vặn:

| Nút vặn | Mặc định | Nghĩa |
|---|---|---|
| **`InstanceLifetime`** | `Singleton` | Vòng đời publisher/subscriber |
| `DefaultAsyncPublishStrategy` | `Parallel` | Chỉ ảnh hưởng `PublishAsync` |
| `EnableCaptureStackTrace` | `false` | Bắt stacktrace debug — bật vào **tốn hiệu năng** |
| `HandlingSubscribeDisposedPolicy` | `Ignore` | Subscribe sau khi dispose: im lặng hay ném lỗi |
| `RequestHandlerLifetime` | `Scoped` | Cho request/response (chưa dùng) |

**Nhưng `RegisterMessageBroker` chỉ đọc ĐÚNG 1 nút:**

```csharp
var lifetime = GetLifetime(options.InstanceLifetime);   // dòng duy nhất dùng options
```

→ Trong ngữ cảnh này, `options` chỉ trả lời: ***"Hộp thư này sống theo vòng đời nào?"*** (mặc định Singleton).

> ⚠️ **Không bao giờ `new MessagePipeOptions()` trong installer.** Cái `options` do Root trả về **chính là instance đã nhét vào container**. Tự tạo cái mới = cầm tờ nội quy khác với tờ bưu điện đang treo → hỏng ngầm, rất khó truy.

### `builder.RegisterMessageBroker<PointerScreenPayload>(options);`

```
builder.RegisterMessageBroker<PointerScreenPayload>(options);
   │                          └── loại tin nào     └── sống thế nào
   └── ghi vào sổ container
```

> *"Ghi sổ: mở kênh gửi/nhận cho tin `PointerScreenPayload`, kiểu Singleton."*

**Nó KHÔNG gửi tin, KHÔNG tạo object ngay.** Chỉ khai báo trước để lát nữa có cái mà đưa.

💡 **Một dòng này đẻ ra 12 đăng ký**, không phải 1:

```
IPublisher<T>              ISubscriber<T>               (+Core)   ← đang dùng
IAsyncPublisher<T>         IAsyncSubscriber<T>          (+Core)
IBufferedPublisher<T>      IBufferedSubscriber<T>       (+Core)   ← xem mục 6
IBufferedAsyncPublisher<T> IBufferedAsyncSubscriber<T>  (+Core)
```

### `return builder;`
Để **nối đuôi** được. Không có vẫn chạy đúng, chỉ là phải viết tách dòng.

```csharp
builder.RegisterInputEvents(options)
       .RegisterMapEvents(options)      // nối được là nhờ return
```

---

## 3. Flow chạy — từ Awake tới lúc nhận event

### Giai đoạn 1 — Khởi động (đúng 1 lần)

```
Scene Preloading load, thấy GameObject [Bootstrap]
│
▼ RootLifetimeScope.Awake()
│     DontDestroyOnLoad(gameObject)      ← container sống xuyên scene
│     base.Awake()
│
▼ VContainer gọi Configure(builder)                    ★ CHỈ KHAI BÁO, chưa tạo gì
│
│   var options = builder.RegisterMessagePipe();       ← xây bưu điện, đẻ ra nội quy
│
│   builder.RegisterInputModule(options)               ← HÀM BẠN VIẾT chạy ở đây
│          .RegisterMapModule(options)                    mỗi module tự khai
│          .RegisterTimeModule(options)                   broker + service global
│          .RegisterStorageModule(options)
│          .RegisterFarmModule(options)
│          .RegisterQuestModule(options);
│
│   ...+ App Block: PlayerDataHolder, Firebase (không thuộc module nào)
│
▼ Configure kết thúc  →  VContainer BUILD container    ★ GIỜ MỚI dựng object thật
      → InputService được `new`, tiêm IPublisher<PointerScreenPayload> vào
```

**Nhớ kỹ:** đọc `Configure` từ trên xuống, **đừng tưởng tượng object đang lần lượt sinh ra**.
Nó chỉ đang **viết công thức**. Bấm nút nấu là ở bước BUILD.

### Giai đoạn 2 — Lúc chơi (mỗi frame)

```
Chuột nhúc nhích
│
▼ InputService.PollPointerScreen()
│     _pubScreen.Publish(new PointerScreenPayload(pos));
│
▼ MessagePipe tra hộp thư PointerScreenPayload
│     gọi ĐỒNG BỘ mọi subscriber đã đăng ký (theo thứ tự subscribe)
│
▼ MapPointerBridge.OnScreen(p)
      → raycast → _map.UpdatePreview(world)
```

---

## 4. Lỗi thường gặp

| Triệu chứng | Nguyên nhân |
|---|---|
| `cannot resolve IPublisher<XxxPayload>` **lúc khởi động** | Quên `RegisterMessageBroker<XxxPayload>` |
| Publish xong **không ai nhận** | Chưa `Subscribe`, hoặc subscriber sinh ra sau lúc publish (→ mục 6) |
| Subscriber **chạy 2 lần** | Subscribe 2 nơi, hoặc quên `Dispose` khi object cũ bị hủy |
| Đổi scene xong **event chết** | Broker đăng ký ở child scope thay vì Root |

> 🟢 **Tin tốt:** thiếu broker thì **văng ngay lúc khởi động**, không phải chờ tới gameplay mới lòi ra.

---

## 5. Thêm payload mới thì sửa file nào?

| Việc | File |
|---|---|
| Thêm payload / service vào module **có sẵn** | Chỉ `{Module}ModuleInstaller.cs` — **không đụng Root** |
| Thêm **module mới** | Tạo `{Module}ModuleInstaller.cs` + thêm 1 dòng ở Root |

Mỗi installer có tối đa **3 hàm**:

| Hàm | Gọi ở đâu | Chứa gì |
|---|---|---|
| `Register{X}Events(options)` | (nội bộ / test) | Chỉ broker |
| `Register{X}Module(options)` | **Root** scope | Broker + service global |
| `Register{X}SceneComponents()` | **Game** scope | Component per-scene |

Đó chính là lý do tách installer: **danh sách hộp thư nằm cạnh chỗ định nghĩa payload**, không bao giờ lệch nhau.

**Quy ước:** 1 payload = **1 chủ sở hữu** = module publish ra nó. Đăng ký trùng ở 2 nơi là hỏng.

---

## 6. Mẹo: `IBufferedSubscriber<T>`

Vấn đề kinh điển: **View sinh ra muộn → lỡ mất event đã publish trước đó.**
(Vì vậy `FarmVisualizer.Start()` phải tự chạy vòng lặp dựng lại các ô đã có.)

`IBufferedSubscriber<T>` khi subscribe sẽ **nhận ngay giá trị publish gần nhất** — sinh ra để giải đúng bài này.
Bạn **không cần đăng ký gì thêm**, nó nằm sẵn trong 12 đăng ký ở mục 2.

---

## 7. Tra cứu: đầy đủ các dạng `builder.Register*`

### 7.1 Bảng quyết định nhanh

| Bạn đang có... | Dùng |
|---|---|
| Class C# thuần, muốn VContainer tự `new` | `Register<T>(Lifetime)` |
| Object/asset **đã tồn tại sẵn** | `RegisterInstance(obj)` |
| MonoBehaviour **đã có trong scene** | `RegisterComponentInHierarchy<T>()` |
| Class cần **tự chạy** lúc khởi động | `RegisterEntryPoint<T>()` |
| Cần **tạo nhiều object lúc runtime** | `RegisterFactory<TParam, T>()` |
| Kênh sự kiện | `RegisterMessageBroker<T>(options)` |

### 7.2 Class C# thuần — VContainer tự dựng

| API | Khi nào dùng |
|---|---|
| `Register<T>(Lifetime)` | Phổ biến nhất. Kèm `.AsImplementedInterfaces()` / `.AsSelf()` |
| `Register<TInterface, TImplement>(Lifetime)` | Chỉ định thẳng 1 interface, khỏi `.As...()` |
| `Register<TI1, TI2, TImplement>(Lifetime)` | Lộ đúng 2 interface |
| `Register(Type, Lifetime)` | Khi type chỉ biết lúc runtime |

**`Lifetime` — 3 mức:**

| Mức | Nghĩa | Dùng khi |
|---|---|---|
| `Singleton` | 1 instance cho cả container | **Mặc định**, hầu hết service |
| `Scoped` | 1 instance mỗi scope (Root riêng, Game riêng) | State reset theo scene |
| `Transient` | Mỗi lần xin là 1 cái mới | Object dùng-rồi-bỏ |

### 7.3 Object đã có sẵn

| API | Ghi chú |
|---|---|
| `RegisterInstance(obj)` | **Không có tham số `Lifetime`** — object đã tồn tại, luôn như Singleton |
| `RegisterInstance<TInterface>(obj)` | Đăng ký dưới 1 interface |
| `RegisterInstance<TI1, TI2>(obj)` / `<TI1, TI2, TI3>` | Nhiều interface |

Dùng cho ScriptableObject: `FarmDatabaseSO`, `QuestCatalogSO`.

### 7.4 MonoBehaviour — khác nhau ở "object đến từ đâu"

| API | Object đến từ đâu |
|---|---|
| `RegisterComponentInHierarchy<T>()` | **TÌM** trong hierarchy của scope ★ dùng nhiều nhất |
| `RegisterComponent<TInterface>(instance)` | Đưa thẳng ref đã có |
| `RegisterComponentOnNewGameObject<T>(lifetime, name)` | **TẠO** GameObject mới + AddComponent |
| `RegisterComponentInNewPrefab<T>(prefab, lifetime)` | **Instantiate** prefab |
| `RegisterComponentInNewPrefab<TInterface, TImplement>(...)` | Instantiate + lộ interface |

⚠️ `RegisterComponentInHierarchy` **chỉ tìm trong hierarchy của LifetimeScope đó** → GameObject phải nằm dưới `[Bootstrap]` (Root) hoặc scope tương ứng.

**Modifier riêng cho component:**

| Modifier | Việc |
|---|---|
| `.UnderTransform(parent)` | Đặt object mới dưới 1 Transform |
| `.UnderTransform(Func<Transform>)` | Tìm parent lúc runtime |
| `.DontDestroyOnLoad()` | Giữ object qua scene |

### 7.5 Entry point & vòng đời

| API | Việc |
|---|---|
| `RegisterEntryPoint<T>()` | Tạo **EAGER** + móc `IStartable`/`ITickable`/`IDisposable` |
| `RegisterEntryPoint<TInterface>()` | Như trên, lộ interface |
| `RegisterEntryPointExceptionHandler(handler)` | Bắt exception văng ra từ entry point |
| `RegisterDisposeCallback(callback)` | Chạy khi scope hủy |
| `RegisterBuildCallback(callback)` | Chạy ngay sau khi build container |

> **Khác biệt cốt lõi:** `Register<T>` = **lazy** (chỉ dựng khi có người xin).
> `RegisterEntryPoint<T>` = **eager** (dựng ngay lúc build). Muốn code tự chạy lúc khởi động → bắt buộc dùng cái này.

### 7.6 Factory — tạo object lúc runtime có tham số

| API | Trả về |
|---|---|
| `RegisterFactory<T>(...)` | `Func<T>` |
| `RegisterFactory<TParam1, T>(...)` | `Func<TParam1, T>` |
| `RegisterFactory<TP1, TP2, T>(...)` | `Func<TP1, TP2, T>` |
| ... tới 4 tham số | |

Dùng khi cần **spawn nhiều instance động** mà vẫn muốn chúng được tiêm dependency (spawn enemy, tạo slot view…).

### 7.7 MessagePipe

| API | Khi nào |
|---|---|
| `RegisterMessagePipe()` / `(configure)` | **1 lần duy nhất** ở Root |
| `RegisterMessageBroker<T>(options)` | Mỗi payload — đẻ ra 12 đăng ký |
| `RegisterMessageBroker<TKey, T>(options)` | Pub/sub có khoá |
| `RegisterRequestHandler<TReq, TRes, TH>()` | Request/response (hỏi-đáp) |
| `RegisterAsyncRequestHandler<...>()` | Request/response bất đồng bộ |
| `RegisterMessageHandlerFilter<TFilter>()` | Chèn filter vào pipeline (log, validate) |
| `RegisterAsyncMessageHandlerFilter<...>()` | Filter cho async |

### 7.8 Modifier chung (nối sau mọi `Register`)

| Modifier | Việc |
|---|---|
| `.AsSelf()` | Đăng ký dưới chính tên class |
| `.AsImplementedInterfaces()` | Đăng ký dưới **mọi** interface nó implement |
| `.As<TInterface>()` (tới 4) | Chỉ định thủ công interface nào |
| `.As(params Type[])` | Như trên, dạng `Type` |
| `.WithParameter<TParam>(value)` | Truyền tay 1 tham số constructor |
| `.WithParameter<TParam>(Func<TParam>)` | Tham số tính lúc resolve |
| `.WithParameter(string name, value)` | Chỉ định theo **tên** tham số |
| `.Keyed(key)` | Đăng ký nhiều bản cùng type, phân biệt bằng khoá |

⚠️ **`AsImplementedInterfaces()` THAY THẾ chứ không cộng dồn** — gọi nó xong mà muốn xin bằng concrete type thì phải thêm `.AsSelf()`.

### 7.9 Bỏ qua — chỉ dành cho DOTS/ECS

`RegisterSystemFromDefaultWorld`, `RegisterSystemFromWorld`, `RegisterSystemIntoWorld`, `RegisterSystemIntoDefaultWorld`, `RegisterNewWorld`, `RegisterUnmanagedSystem*` — project này không dùng Entities.

### 7.10 Không cần gọi

`RegisterContainer()` — VContainer **tự động** đăng ký `IObjectResolver` trong `BuildRegistry()`:

```csharp
registrations[^1] = new Registration(typeof(IObjectResolver), Lifetime.Transient, null, ContainerInstanceProvider.Default);
```

Đó là lý do `QuestTestPanelBootstrap` inject được `IObjectResolver` mà không đăng ký gì.

---

## 8. Xem registration trong Editor

VContainer có cửa sổ soi toàn bộ registration — **mặc định TẮT**.

### Bật (làm 1 lần)

| Bước | Việc |
|---|---|
| 1 | `Assets > Create > VContainer > VContainer Settings` → chọn nơi lưu |
| 2 | Chọn asset vừa tạo → Inspector → tick ☑ **`Enable Diagnostics`** |
| 3 | `Window > VContainer Diagnostics` |
| 4 | **Bấm Play** — cửa sổ chỉ có dữ liệu lúc runtime |

> Bước 1 **tự động** thêm asset vào `PlayerSettings > Preloaded Assets` — đó là cách `VContainerSettings.Instance` được nạp. **Phải tạo qua menu**, tạo file tay không ăn.

⚠️ **Thứ tự quan trọng:** cờ được đọc lúc `LifetimeScope` build container:
```csharp
builder.Diagnostics = VContainerSettings.DiagnosticsEnabled ? DiagnositcsContext.GetCollector(scopeName) : null;
```
→ phải bật **TRƯỚC khi vào Play**. Đang Play mới tick thì thoát ra Play lại.

### Đọc bảng

| Cột | Nghĩa |
|---|---|
| `Type` | Class được đăng ký |
| `ContractTypes` | Các interface xin được (kết quả của `.AsImplementedInterfaces()`) |
| `Lifetime` | Singleton / Scoped / Transient |
| `Register` | Kiểu đăng ký (`OpenGeneric`, `Component`, `Instance`…) |
| `RefCount` | **Số instance đã dựng thật.** `0` = mới chỉ là công thức |
| `Scope` | Root hay Game |

### Lọc bớt nhiễu

Cửa sổ hiện cả hạ tầng MessagePipe. Cách phân biệt:

| Dòng | Là gì |
|---|---|
| `MessageBroker<`**`TMessage`**`>` | Khuôn mẫu hệ thống (từ `RegisterMessagePipe`) |
| `MessageBroker<`**`ServerTimeSyncedPayload`**`>` | ✅ **Của bạn** (từ `RegisterMessageBroker<T>`) |

> **Thấy `<TMessage>` / `<TKey,TMessage>` → hệ thống. Thấy tên payload cụ thể → của bạn.**

3 cách lọc:
1. **Ô Search** ở góc **phải** toolbar — lọc theo cột `Type`. Gõ `Quest`, `Farm`…
2. **Click header cột `Register`** → rác hệ thống đều là `OpenGeneric`, gom thành một cụm.
3. **Sort `RefCount`** → rác hệ thống toàn `0`.

Mớ open-generic đó **không xoá được và cũng không nên xoá** — MessagePipe cần khuôn `<TMessage>` để "đúc" ra bản cụ thể. `RefCount = 0` nghĩa là chưa dựng object nào, không tốn RAM đáng kể.

⚠️ Tooltip của package cảnh báo *"Note: Performance degradation"* → **tắt trước khi build release**.

---

## 9. Quy tắc cần nhớ

1. **`RegisterMessagePipe()` gọi ĐÚNG 1 LẦN ở Root.** Installer chỉ *nhận* `options`, không bao giờ tự gọi.
2. **`Configure` là viết công thức, không phải nấu ăn.** Object chỉ sinh ra lúc BUILD.
3. **1 payload = 1 chủ sở hữu.** Đăng ký ở đúng module publish ra nó.
4. **Publish là ĐỒNG BỘ.** Đừng `Publish` khi đang lặp trên state có thể bị đổi — gom vào list tạm, thoát vòng lặp rồi mới publish.
5. **Payload chỉ chứa thứ publisher tự biết.** Nhét field chỉ để phục vụ 1 consumer = làm bẩn hợp đồng dùng chung.
