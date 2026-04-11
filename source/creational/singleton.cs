// ============================================================
// Singleton Pattern — C# Example
// ============================================================
//
// Intent: Ensure a class has only one instance and provide a
// global point of access to it.
// Pros:
//  1. Controlled access to sole instance — only one object is ever created.
//  2. Lazy initialisation — instance is created only when first needed, saving resources.
//  3. Reduced memory footprint — avoids repeated allocation of heavy objects (e.g. DB connections).
//  4. Global access point — convenient when a shared resource must be reachable across the codebase.
//  5. Thread safety (with Lazy<T> or static readonly) — no race conditions on creation.
// Cons:
//  1. Global state can lead to hidden coupling and makes dependencies implicit.
//  2. Hard to unit test — replacing or mocking a singleton requires extra effort or interfaces.
//  3. Violates Single Responsibility Principle — the class manages both its logic and its lifetime.
//  4. Can be a bottleneck in high-concurrency scenarios if the instance holds shared mutable state.
//  5. Hinders dependency injection — tight coupling to a concrete class instead of an abstraction.
//
// Three approaches shown:
//   1. Lazy<T>          — recommended, thread-safe, lazy initialisation
//   2. Static readonly  — thread-safe, eager initialisation
//   3. Double-check lock — shown for reference; Lazy<T> is preferred
// ============================================================
#region  ── Approach 1: Lazy<T> — recommended ─────────────────────
// Lazy<T> guarantees thread-safe, one-time initialisation.
// The instance is not created until first access.
// Note: In production code, prefer registering the singleton with the DI container
// rather than using static access — this avoids hidden global-state coupling.
// Example: Application configuration loaded once and shared across the app.
// Note: The class is sealed to prevent inheritance, which could lead to multiple instances.
// Note: The constructor is private to prevent external instantiation.
// Note: The settings are stored in a dictionary for demonstration purposes.
// Pros:
//  1. Thread-safe by default — no manual locking required; CLR guarantees single initialisation.
//  2. Truly lazy — instance is created only on first access, not at class-load time.
//  3. Exception handling — if the factory throws, subsequent accesses retry by default (configurable).
//  4. Clean syntax — intent is explicit; no boilerplate double-check lock needed.
//  5. Testable — LazyThreadSafetyMode can be set to None in tests to disable overhead.
// Cons:
//  1. Slight indirection — access via _lazy.Value adds a layer that may confuse readers unfamiliar with Lazy<T>.
//  2. Cannot reset — once initialised, Lazy<T> cannot be reset; a new instance is needed for re-initialisation.
//  3. Overhead — carries a small allocation and a volatile read on every access (usually negligible).
//  4. Exception caching — by default, a faulted Lazy<T> caches the exception and re-throws on every access.
//  5. Not suitable for async initialisation — Lazy<T> is synchronous; use AsyncLazy or similar for async factories.

sealed class AppConfiguration
{
    private static readonly Lazy<AppConfiguration> _lazy =
        new(() => new AppConfiguration());

    public static AppConfiguration Instance => _lazy.Value;

    private readonly Dictionary<string, string> _settings = new();

    private AppConfiguration()
    {
        // Simulate loading from disk / environment
        _settings["DatabaseUrl"] = "Server=prod-db;Database=app;";
        _settings["MaxConnections"] = "100";
        _settings["LogLevel"] = "Warning";
        Console.WriteLine("  AppConfiguration initialised (once).");
    }

    public string Get(string key) =>
        _settings.TryGetValue(key, out var value) ? value : $"<missing:{key}>";
}

#endregion

#region  ── Approach 2: Static readonly — eager, also thread-safe ──
// CLR guarantees that static field initialisers run exactly once.
// Pros:
//  1. Simplest syntax — a single field declaration; no factory lambda or lock required.
//  2. Thread-safe by CLR guarantee — static field initialisers run exactly once, atomically.
//  3. No lock overhead — no synchronisation cost on every access after initialisation.
//  4. Deterministic initialisation — instance is ready before any code in the class can run.
//  5. Immutable reference — readonly prevents the field from being reassigned accidentally.
// Cons:
//  1. Eager initialisation — instance is created at class-load time even if never used, wasting resources.
//  2. No lazy loading — cannot defer expensive construction until the instance is actually needed.
//  3. Cannot pass constructor arguments — the initialiser runs before any external data is available.
//  4. Harder to reset or replace in tests — readonly field cannot be swapped without reflection.
//  5. Initialisation exceptions are wrapped — a thrown exception becomes a TypeInitializationException, obscuring the root cause.
class MetricsCollector
{
    public static readonly MetricsCollector Instance = new();

    private int _requestCount;

    private MetricsCollector()
    {
        Console.WriteLine("  MetricsCollector initialised (once).");
    }

    public void RecordRequest() => Interlocked.Increment(ref _requestCount);
    public int TotalRequests => _requestCount;
}

#endregion

#region  ── Approach 3: Double-check lock (for reference only) ─────
// Use Lazy<T> instead — this approach is shown only to illustrate
// what Lazy<T> replaces.
class LegacySingleton
{
    private static LegacySingleton? _instance;
    private static readonly object _lock = new();

    private LegacySingleton() { }

    public static LegacySingleton Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new LegacySingleton();
                }
            }
            return _instance;
        }
    }
}
#endregion

#region  ── Approach 4: Simple Scenario ─────────────────────────
// Business requirement: 
// I need to call external API, but I want to limit the number of calls to 1 per minute.
// I want to share the same instance of the API client across my application, but I also want to ensure that only one instance is created and used throughout the application.
sealed class ApiClient
{
    private static ApiClient? _instance = new();

    private DateTime _lastCallTime = DateTime.MinValue;

    public static ApiClient Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new ApiClient();
            }
            return _instance;
        }
    }

    private ApiClient()
    {
        Console.WriteLine("  ApiClient initialised (once).");
    }

    public void CallApi()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastCallTime).TotalSeconds < 60)
        {
            Console.WriteLine("  API call blocked: rate limit exceeded.");
            return;
        }

        // Simulate API call
        Console.WriteLine("  API call made.");
        _lastCallTime = now;
    }
}
#endregion


// ── Example usage ───────────────────────────────────────────
class Program
{
    static void Main()
    {
        // ── Demo ───────────────────────────────────────────────────
        Console.WriteLine("=== Singleton Pattern ===\n");

        Console.WriteLine("--- Approach 1: Lazy<T> ---");
        //var cfg1 = new AppConfiguration();  // Error: constructor is private
        var cfg1 = AppConfiguration.Instance;
        var cfg2 = AppConfiguration.Instance;
        Console.WriteLine($"Same instance? {ReferenceEquals(cfg1, cfg2)}");  // True
        Console.WriteLine($"DatabaseUrl:   {AppConfiguration.Instance.Get("DatabaseUrl")}");

        Console.WriteLine("\n--- Approach 2: Static readonly ---");
        // Instance already created when class is loaded
        MetricsCollector.Instance.RecordRequest();
        MetricsCollector.Instance.RecordRequest();
        MetricsCollector.Instance.RecordRequest();
        Console.WriteLine($"Total requests: {MetricsCollector.Instance.TotalRequests}"); // 3

        Console.WriteLine("\n--- Approach 3: Double-check lock (for reference only) ---");
        var legacy1 = LegacySingleton.Instance;
        var legacy2 = LegacySingleton.Instance;
        Console.WriteLine($"Same instance? {ReferenceEquals(legacy1, legacy2)}");  // True

        Console.WriteLine("\n--- Approach 4: Simple Scenario ---");
        var apiClient1 = ApiClient.Instance;
        var apiClient2 = ApiClient.Instance;
        Console.WriteLine($"Same instance? {ReferenceEquals(apiClient1, apiClient2)}");  // True
        apiClient1.CallApi();  // API call made.
        apiClient2.CallApi();  // API call blocked: rate limit exceeded.

        // ── Prefer DI over static access in application code ──────
        Console.WriteLine("\nTip: In production code, register the singleton with the DI container:");
        Console.WriteLine("  services.AddSingleton<AppConfiguration>();");
        Console.WriteLine("Then inject it via constructor — avoids hidden global-state coupling.");
    }
}