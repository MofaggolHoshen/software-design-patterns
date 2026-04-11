# 🔒 Singleton Pattern

The Singleton pattern ensures a class has **only one instance** throughout the lifetime of the application and provides a **global access point** to it. It is one of the simplest GoF patterns but also one of the most abused — understanding when it is appropriate is as important as knowing how to implement it correctly.

## Intent

> Ensure a class has only one instance, and provide a global point of access to it.

## Problem

Some resources — configuration stores, connection pools, logger factories — should exist only once. Without a deliberate mechanism, multiple parts of the code can each instantiate their own copies, leading to inconsistent state, wasted resources, or race conditions. A naive `static` field without thread-safety protection causes subtle multi-threading bugs.

### Bad Example

```csharp
// Thread-unsafe singleton: two threads can both see _instance == null
// and each create their own instance. Double-check locking done incorrectly.
class ConfigurationManager
{
    private static ConfigurationManager? _instance;

    private ConfigurationManager()
    {
        Console.WriteLine("Loading configuration from disk...");
    }

    public static ConfigurationManager Instance
    {
        get
        {
            if (_instance == null)                     // ← race condition here
                _instance = new ConfigurationManager();
            return _instance;
        }
    }

    public string GetSetting(string key) => $"value-of-{key}";
}
```

### Good Example

```csharp
// ── Option 1: Lazy<T> — recommended, thread-safe by default ──
class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _lazy =
        new(() => new ConfigurationManager());

    // InstanceId is assigned once inside the constructor.
    // No matter how many variables point to the singleton,
    // they all see the same GUID — proving one instance exists.
    public Guid InstanceId { get; } = Guid.NewGuid();

    // Private constructor prevents external instantiation
    private ConfigurationManager()
    {
        Console.WriteLine($"Configuration loaded (once). InstanceId = {InstanceId}");
    }

    public static ConfigurationManager Instance => _lazy.Value;

    public string GetSetting(string key) => $"value-of-{key}";
}

// ── Option 2: Static initialiser — also thread-safe ──────────
class LoggerFactory
{
    // CLR guarantees static field initialisation is thread-safe
    public static readonly LoggerFactory Instance = new();

    public Guid InstanceId { get; } = Guid.NewGuid();

    private LoggerFactory()
    {
        Console.WriteLine($"LoggerFactory initialised (once). InstanceId = {InstanceId}");
    }

    public void Log(string message) => Console.WriteLine($"[LOG] {message}");
}

// ── Usage ─────────────────────────────────────────────────
var cfg1 = ConfigurationManager.Instance;
var cfg2 = ConfigurationManager.Instance;

// Both variables hold the same GUID — only one instance was ever created.
Console.WriteLine(cfg1.InstanceId);                   // e.g. 5943aac6-e0a9-...
Console.WriteLine(cfg2.InstanceId);                   // same GUID
Console.WriteLine(cfg1.InstanceId == cfg2.InstanceId); // True
Console.WriteLine(ReferenceEquals(cfg1, cfg2));        // True — same instance

ConfigurationManager.Instance.GetSetting("ConnectionString");
LoggerFactory.Instance.Log("Application started");
```

### Proving a Single Instance with a GUID

Each singleton class exposes a `public Guid InstanceId { get; } = Guid.NewGuid();` property. `Guid.NewGuid()` generates a **universally unique value at object construction time**. Because `InstanceId` is assigned inside the object initialiser (before the constructor body runs), it is set exactly once when the instance is first created.

No matter how many variables reference the singleton — `cfg1`, `cfg2`, or a call to `Instance` minutes later — they all read the **same GUID**, which is concrete, observable proof that only a single instance was ever constructed.

```
=== Singleton Pattern ===

--- Approach 1: Lazy<T> ---
  AppConfiguration initialised (once). InstanceId = 5943aac6-e0a9-4969-be4d-24e2ac374d50
  cfg1.InstanceId = 5943aac6-e0a9-4969-be4d-24e2ac374d50
  cfg2.InstanceId = 5943aac6-e0a9-4969-be4d-24e2ac374d50
  Same GUID?      True
  Same instance?  True
```

> If two instances were ever created (i.e., the pattern broke), each would produce a **different** GUID and the `Same GUID?` check would print `False`.

## Approaches

### Approach 1 — `Lazy<T>` ⭐ Recommended

`Lazy<T>` is the **idiomatic, modern way** to implement a singleton in C#. The CLR guarantees that the factory lambda runs exactly once, even under concurrent access, without any manual locking.

```csharp
sealed class AppConfiguration
{
    private static readonly Lazy<AppConfiguration> _lazy =
        new(() => new AppConfiguration());

    public static AppConfiguration Instance => _lazy.Value;

    // Assigned once at object construction — same value on every access proves one instance.
    public Guid InstanceId { get; } = Guid.NewGuid();

    private AppConfiguration()
    {
        Console.WriteLine($"  AppConfiguration initialised (once). InstanceId = {InstanceId}");
    }
}

// Proof:
var cfg1 = AppConfiguration.Instance;
var cfg2 = AppConfiguration.Instance;
Console.WriteLine(cfg1.InstanceId);                    // e.g. 5943aac6-e0a9-...
Console.WriteLine(cfg2.InstanceId);                    // same GUID
Console.WriteLine(cfg1.InstanceId == cfg2.InstanceId); // True
Console.WriteLine(ReferenceEquals(cfg1, cfg2));         // True
```

**Why it is recommended:**
- Thread-safe out of the box — no `lock`, no `volatile`, no race condition.
- Truly lazy — the instance is not created until `Instance` is first accessed.
- `sealed` prevents subclasses from breaking the single-instance guarantee.
- Private constructor prevents external `new` calls.
- The printed GUID proves that `AppConfiguration` is constructed exactly once — any two variables will report the same `InstanceId`.

---

### Approach 2 — `static readonly` (Eager Initialisation)

The CLR guarantees that static field initialisers run exactly once before any code in the class executes. This is the **simplest** approach when you are happy to pay the construction cost at class-load time.

```csharp
class MetricsCollector
{
    public static readonly MetricsCollector Instance = new();

    // Same GUID proof: assigned once when the class is first loaded.
    public Guid InstanceId { get; } = Guid.NewGuid();

    private MetricsCollector()
    {
        Console.WriteLine($"  MetricsCollector initialised (once). InstanceId = {InstanceId}");
    }

    public void RecordRequest() => Interlocked.Increment(ref _requestCount);
    public int TotalRequests => _requestCount;
    private int _requestCount;
}

// Proof:
var mc1 = MetricsCollector.Instance;
var mc2 = MetricsCollector.Instance;
Console.WriteLine(mc1.InstanceId == mc2.InstanceId); // True — same GUID, same instance
```

**Trade-off vs `Lazy<T>`:**
- Slightly faster (no `_lazy.Value` indirection).
- No lazy loading — instance is always created, even if never used.
- Good choice for lightweight objects that are almost certainly needed.
- GUID is still the same regardless of when or how many times `Instance` is accessed.

---

### Approach 3 — Double-Check Lock ⚠️ Legacy / Reference Only

Shown for historical context. Before `Lazy<T>` was available, a double-check lock was the standard thread-safe pattern. Today `Lazy<T>` is strongly preferred because it is less error-prone.

```csharp
class LegacySingleton
{
    private static LegacySingleton? _instance;
    private static readonly object _lock = new();

    // Same GUID proof works here too.
    public Guid InstanceId { get; } = Guid.NewGuid();

    private LegacySingleton()
    {
        Console.WriteLine($"  LegacySingleton initialised (once). InstanceId = {InstanceId}");
    }

    public static LegacySingleton Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new LegacySingleton();   // safe double-check
                }
            }
            return _instance;
        }
    }
}

// Proof:
var l1 = LegacySingleton.Instance;
var l2 = LegacySingleton.Instance;
Console.WriteLine(l1.InstanceId == l2.InstanceId); // True
Console.WriteLine(ReferenceEquals(l1, l2));         // True
```

**Why to avoid today:**
- Verbose and error-prone; easy to get the double-check wrong.
- `Lazy<T>` replaces this pattern cleanly with less code.
- Still correct when written carefully, but not idiomatic in modern C#.

---

### Approach 4 — Practical Scenario (Rate-Limited API Client)

Demonstrates a real-world use case: an external API client where at most one call per minute is allowed. The singleton ensures the last-call timestamp is shared across the entire application.

```csharp
sealed class ApiClient
{
    private static readonly Lazy<ApiClient> _lazy = new(() => new ApiClient());
    public static ApiClient Instance => _lazy.Value;

    // Same GUID proof.
    public Guid InstanceId { get; } = Guid.NewGuid();

    private DateTime _lastCallTime = DateTime.MinValue;

    private ApiClient()
    {
        Console.WriteLine($"  ApiClient initialised (once). InstanceId = {InstanceId}");
    }

    public void CallApi()
    {
        if ((DateTime.UtcNow - _lastCallTime).TotalSeconds < 60)
        {
            Console.WriteLine("API call blocked: rate limit exceeded.");
            return;
        }
        Console.WriteLine("API call made.");
        _lastCallTime = DateTime.UtcNow;
    }
}

// Proof:
var a1 = ApiClient.Instance;
var a2 = ApiClient.Instance;
Console.WriteLine(a1.InstanceId == a2.InstanceId); // True — same client, same rate-limit state
a1.CallApi(); // API call made.
a2.CallApi(); // API call blocked: rate limit exceeded.
```

**Key insight:** the singleton lifetime keeps `_lastCallTime` alive for the duration of the process, so the rate limit is enforced globally, not per-caller. The identical `InstanceId` from `a1` and `a2` confirms they share the same state.

---

### Approach 5 — Dependency Injection ⭐⭐ Recommended for Production

In application code, **avoid static access entirely**. Register the type as a singleton with the DI container and inject it via constructor. This is the most testable and maintainable approach.

```csharp
// Registration (Startup / Program.cs)
builder.Services.AddSingleton<AppConfiguration>();
builder.Services.AddSingleton<MetricsCollector>();

// Consumption — no static access, no hidden coupling
class OrderService
{
    private readonly AppConfiguration _config;

    public OrderService(AppConfiguration config)   // injected by DI
    {
        _config = config;
    }
}
```

**Why it is the best practice:**
- Dependencies are **explicit** — visible in the constructor signature.
- Fully **testable** — swap the real implementation with a mock by registering a different type.
- Lifetime is managed by the container; no boilerplate in the class itself.
- Works seamlessly with `IOptions<T>`, `ILogger<T>`, and other framework abstractions.

> **Rule of thumb:** Use `Lazy<T>` (Approach 1) in library or utility code with no DI container.  
> Use DI registration (Approach 5) in application code (ASP.NET Core, Worker Services, etc.).

---

## Key Takeaways

- `Lazy<T>` is the idiomatic thread-safe singleton in modern C#.
- A static readonly field also works and is slightly faster (no lazy overhead) if eager initialisation is acceptable.
- Singleton makes unit testing harder — it introduces hidden global state. Prefer registering the instance with a DI container (`services.AddSingleton<T>()`) so tests can inject a different instance.
- Serialisation and reflection can bypass the private constructor — guard if needed.

## When to Use

- A resource must be shared and exactly one instance is required by design (e.g., a configuration store, connection pool, or cache).
- You are using a DI container and simply want to express single-instance lifetime — use `AddSingleton`, not the pattern itself.

## When NOT to Use

- The "global" access hides dependencies — prefer constructor injection so dependencies are explicit.
- Multiple isolated instances are needed during testing.
- The instance holds mutable state accessed from many unrelated parts of the codebase (this creates hidden coupling).

## Pros and Cons

### Pros

| # | Benefit | Detail |
|---|---------|--------|
| 1 | **Controlled single instance** | Guarantees exactly one object is ever created, preventing duplicate resource allocation. |
| 2 | **Lazy initialisation** | With `Lazy<T>`, the instance is created only on first access — no cost if never used. |
| 3 | **Reduced memory footprint** | Avoids repeatedly allocating heavy objects such as DB connection pools or config stores. |
| 4 | **Global access point** | A shared resource is conveniently reachable across the codebase without passing it explicitly. |
| 5 | **Thread safety** | `Lazy<T>` and `static readonly` both guarantee single initialisation without manual locking. |

### Cons

| # | Drawback | Detail |
|---|----------|--------|
| 1 | **Hidden global state** | Classes that use a singleton carry an implicit dependency, making behaviour hard to reason about. |
| 2 | **Difficult to unit test** | Replacing or mocking a singleton requires interfaces, wrapper abstractions, or reflection. |
| 3 | **Violates SRP** | The class is responsible for both its own logic and managing its own lifetime. |
| 4 | **Concurrency bottleneck** | A singleton holding shared mutable state can become a contention point under high load. |
| 5 | **Hinders dependency injection** | Static access couples code to a concrete type instead of an abstraction, undermining DI benefits. |
