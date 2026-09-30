# 🎀 Decorator Pattern

The Decorator pattern **attaches additional responsibilities** to an object dynamically. It provides a flexible alternative to subclassing for extending functionality by wrapping an object in one or more decorator objects. 

## Intent

> Attach additional responsibilities to an object dynamically. Decorators provide a flexible alternative to subclassing for extending functionality.

## Problem

When you need to add optional behaviours to objects (logging, caching, compression, validation), creating a subclass for every combination leads to a class explosion. Twelve features on a base class would require up to 2¹² = 4,096 subclasses to cover all combinations.

### Bad Example

```csharp
class DataService  { public virtual string Read()  => "raw data"; }

// Must create one subclass per combination:
class CachedDataService     : DataService { /* cache */ }
class LoggedDataService     : DataService { /* log   */ }
class CachedLoggedService   : DataService { /* both  */ } // ← combinatorial explosion
class CompressedDataService : DataService { /* compress */ }
class CachedCompressedLoggedService : DataService { /* ... */ } // n^2 problem
```

### Good Example

```csharp
// ── Component interface ───────────────────────────────────
interface IDataService
{
    string Read(string key);
    void   Write(string key, string value);
}

// ── Concrete Component ────────────────────────────────────
class DatabaseService : IDataService
{
    private readonly Dictionary<string, string> _db = new();

    public string Read(string key)
    {
        Console.WriteLine($"  [DB]    Reading  '{key}'");
        return _db.TryGetValue(key, out var v) ? v : "<not found>";
    }

    public void Write(string key, string value)
    {
        Console.WriteLine($"  [DB]    Writing  '{key}' = '{value}'");
        _db[key] = value;
    }
}

// ── Base Decorator ────────────────────────────────────────
abstract class DataServiceDecorator(IDataService inner) : IDataService
{
    public virtual string Read(string key)              => inner.Read(key);
    public virtual void   Write(string key, string val) => inner.Write(key, val);
}

// ── Concrete Decorators ───────────────────────────────────
class CachingDecorator(IDataService inner) : DataServiceDecorator(inner)
{
    private readonly Dictionary<string, string> _cache = new();

    public override string Read(string key)
    {
        if (_cache.TryGetValue(key, out var hit))
        {
            Console.WriteLine($"  [Cache] HIT      '{key}'");
            return hit;
        }
        var value = base.Read(key);
        _cache[key] = value;
        return value;
    }

    public override void Write(string key, string val)
    {
        _cache.Remove(key);   // invalidate
        base.Write(key, val);
    }
}

class LoggingDecorator(IDataService inner) : DataServiceDecorator(inner)
{
    public override string Read(string key)
    {
        Console.WriteLine($"  [Log]   START Read '{key}'");
        var result = base.Read(key);
        Console.WriteLine($"  [Log]   END   Read '{key}' → '{result}'");
        return result;
    }
}

// ── Demo: compose decorators at runtime ───────────────────
Console.WriteLine("=== Decorator Pattern ===\n");

// DB → Cache → Log  (outermost wraps innermost)
IDataService service =
    new LoggingDecorator(
        new CachingDecorator(
            new DatabaseService()));

service.Write("user:1", "Alice");
Console.WriteLine();

service.Read("user:1");   // cache miss → DB read
Console.WriteLine();
service.Read("user:1");   // cache hit — DB not called
```

## Key Takeaways

- Behaviours can be mixed and matched at runtime — no subclass explosion.
- Each decorator has a single responsibility (SRP).
- The wrapped object is unaware of its decorators.
- .NET's `Stream` hierarchy (`GZipStream`, `BufferedStream`, `CryptoStream`) is a built-in example of this pattern.

## When to Use

- You need to add responsibilities to objects at runtime without affecting other objects.
- Extension by subclassing is impractical (sealed classes, combinatorial explosion).
- You want a composable pipeline of cross-cutting concerns (logging, caching, retries).

## When NOT to Use

- The decorator order matters and is not obvious — document or test it carefully.
- The number of decorators is fixed and known at compile time — a simpler approach (composition in the class itself) may be clearer.
