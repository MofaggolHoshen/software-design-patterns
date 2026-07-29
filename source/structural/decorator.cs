// ============================================================
// Decorator Pattern — C# Example
// ============================================================
//
// Intent: Attach additional responsibilities to an object
// dynamically — a flexible alternative to subclassing.
//
// Key roles:
//   IDataService              — Component interface
//   DatabaseService           — Concrete Component
//   DataServiceDecorator      — Abstract Decorator
//   CachingDecorator          — Concrete Decorator
//   LoggingDecorator          — Concrete Decorator
//   CompressionDecorator      — Concrete Decorator
// ============================================================

// ── Component interface ───────────────────────────────────
interface IDataService
{
    string Read(string key);
    void Write(string key, string value);
}

// ── Concrete Component ────────────────────────────────────
class DatabaseService : IDataService
{
    private readonly Dictionary<string, string> _db = new();

    public string Read(string key)
    {
        Console.WriteLine($"  [DB]       Reading  '{key}'");
        Thread.Sleep(10); // simulate latency
        return _db.TryGetValue(key, out var v) ? v : "<not found>";
    }

    public void Write(string key, string value)
    {
        Console.WriteLine($"  [DB]       Writing  '{key}' = '{value}'");
        _db[key] = value;
    }
}

// ── Base Decorator ────────────────────────────────────────
abstract class DataServiceDecorator(IDataService inner) : IDataService
{
    public virtual string Read(string key) => inner.Read(key);
    public virtual void Write(string key, string val) => inner.Write(key, val);
}

// ── Decorator 1: Memory Cache ────────────────────────────
class CachingDecorator(IDataService inner) : DataServiceDecorator(inner)
{
    private readonly Dictionary<string, string> _cache = new();

    public override string Read(string key)
    {
        if (_cache.TryGetValue(key, out var hit))
        {
            Console.WriteLine($"  [Cache]    HIT  '{key}'");
            return hit;
        }
        var value = base.Read(key);
        _cache[key] = value;
        return value;
    }

    public override void Write(string key, string val)
    {
        _cache.Remove(key); // invalidate on write
        base.Write(key, val);
    }
}

// ── Decorator 2: Logging ────────────────────────────────
class LoggingDecorator(IDataService inner) : DataServiceDecorator(inner)
{
    private static int _callCount;

    public override string Read(string key)
    {
        Console.WriteLine($"  [Log #{++_callCount}]  START Read('{key}')");
        var result = base.Read(key);
        Console.WriteLine($"  [Log]      END   Read('{key}') = '{result}'");
        return result;
    }

    public override void Write(string key, string val)
    {
        Console.WriteLine($"  [Log #{++_callCount}]  Write('{key}', '{val}')");
        base.Write(key, val);
    }
}

// ── Decorator 3: Compression (simulated) ─────────────────
class CompressionDecorator(IDataService inner) : DataServiceDecorator(inner)
{
    private string Compress(string v) => $"[compressed:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(v))}]";
    private string Decompress(string v) => v.StartsWith("[compressed:")
        ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(v[12..^1]))
        : v;

    public override string Read(string key)
    {
        var compressed = base.Read(key);
        return Decompress(compressed);
    }

    public override void Write(string key, string val) =>
        base.Write(key, Compress(val));
}

// ── Demo: compose decorators ─────────────────────────────
Console.WriteLine("=== Decorator Pattern ===\n");

// Stack: Logging → Caching → Compression → DB
IDataService service =
    new LoggingDecorator(
        new CachingDecorator(
            new CompressionDecorator(
                new DatabaseService())));

Console.WriteLine("--- Write ---");
service.Write("user:1", "Alice");

Console.WriteLine("\n--- First Read (cache miss) ---");
var val1 = service.Read("user:1");
Console.WriteLine($"  Got: {val1}");

Console.WriteLine("\n--- Second Read (cache hit) ---");
var val2 = service.Read("user:1");
Console.WriteLine($"  Got: {val2}");
