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

    // Private constructor prevents external instantiation
    private ConfigurationManager()
    {
        Console.WriteLine("Configuration loaded (once).");
    }

    public static ConfigurationManager Instance => _lazy.Value;

    public string GetSetting(string key) => $"value-of-{key}";
}

// ── Option 2: Static initialiser — also thread-safe ──────────
class LoggerFactory
{
    // CLR guarantees static field initialisation is thread-safe
    public static readonly LoggerFactory Instance = new();

    private LoggerFactory() { }

    public void Log(string message) => Console.WriteLine($"[LOG] {message}");
}

// ── Usage ─────────────────────────────────────────────────
var cfg1 = ConfigurationManager.Instance;
var cfg2 = ConfigurationManager.Instance;
Console.WriteLine(ReferenceEquals(cfg1, cfg2)); // True — same instance

ConfigurationManager.Instance.GetSetting("ConnectionString");
LoggerFactory.Instance.Log("Application started");
```

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
