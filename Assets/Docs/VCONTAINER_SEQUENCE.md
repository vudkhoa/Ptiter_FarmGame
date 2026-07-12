# VContainer — Sơ đồ trình tự chạy (tổng quát)

```
Scene chứa LifetimeScope load
│
▼
LifetimeScope.Awake()
│   (Root scope: DontDestroyOnLoad → container sống xuyên scene)
│   base.Awake()  ──►  BUILD container
│
├─ Configure(builder):                              ← KHAI BÁO, chưa tạo gì
│     RegisterMessagePipe() + RegisterMessageBroker<TPayload>()      (kênh pub/sub)
│     Register<TService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
│     RegisterComponentInHierarchy<TMono>()                          (service là MonoBehaviour)
│     RegisterInstance(asset)                                        (SO/instance có sẵn)
│
▼  (1) RESOLVE + CONSTRUCT  — tạo instance, tiêm dependency qua CONSTRUCTOR
│       new TService(depA, depB, ...)     ← DI tự resolve từng tham số
│       • Entry-point (IStartable / IAsyncStartable): tạo EAGER ngay lúc build
│       • Service thường: tạo LAZY, chỉ khi lần đầu bị resolve
│
▼  (2) ENTRY-POINT DISPATCH  — theo vòng đời, đúng thứ tự đăng ký
│       IInitializable.Initialize()        (đồng bộ, sớm nhất)
│       IStartable.Start()                 (đồng bộ)
│       IAsyncStartable.StartAsync(ct)     (async — await được việc nặng: đọc save, mạng...)
│       ITickable.Tick() / IFixedTickable  (mỗi frame, nếu có)
│
▼  APP CHẠY — các service nói chuyện qua interface đã đăng ký
│       (IPublisher/ISubscriber, IXxxService...)  — KHÔNG new tay, KHÔNG FindObject
│
▼  Container / scope bị hủy (đổi scene Single, tắt app)
│       IDisposable.Dispose()   ← VContainer TỰ gọi → gỡ subscription, giải phóng
│
──────────────────────────────────────────────────────────────
Nhiều scope (cha–con):

LifetimeScope A  (Root, DontDestroyOnLoad)
    └─ LifetimeScope B  (per-scene) —  parent = A
          → KẾ THỪA mọi registration của A
          → resolve được cả service của A lẫn của B
```

---

### Chú thích các pha

| Pha | Khi nào | Ai gọi |
|-----|---------|--------|
| **Configure** | trong `Awake` lúc build | bạn viết (chỉ khai báo) |
| **(1) Construct** | ngay sau build | VContainer `new` + inject constructor |
| **(2) Dispatch** | ngay sau construct | VContainer gọi `Initialize`/`Start`/`StartAsync`/`Tick` |
| **Dispose** | khi scope hủy | VContainer gọi `Dispose` của `IDisposable` |

### Quy tắc cần nhớ

1. **Construct luôn trước Start** — có đủ dependency (constructor) rồi mới chạy logic (StartAsync).
2. **`.AsImplementedInterfaces()`** để lộ `IStartable`/`IAsyncStartable`/`IDisposable`... cho container → mới được auto-gọi. Quên → service không tự chạy.
3. **Entry-point = eager, service thường = lazy.** Muốn code chạy lúc khởi động → implement `IStartable`/`IAsyncStartable`.
4. **Async yield** — khi một `StartAsync` `await`, nó nhả quyền → entry-point kế tiếp kịp chạy. Vì vậy service phụ thuộc nhau nên dùng **cờ trạng thái + event** ("ready-hoặc-chờ") thay vì giả định thứ tự.

### Map vào project này

- **Root scope:** `RootLifetimeScope` (scene Preloading, DontDestroyOnLoad) — đăng ký service global.
- **Child scope:** `GameLifetimeScope` (scene Game) — `parent = Root`, đăng ký service per-scene (Farm, Map...).
- **Ví dụ entry-point cụ thể** (luồng riêng của từng hệ): xem doc của hệ đó, vd `FIREBASE_FLOW.md`.
