# 🪶 Flyweight Pattern

The Flyweight pattern uses **sharing** to efficiently support a large number of fine-grained objects. It separates extrinsic state (context-specific, passed by clients) from intrinsic state (shared, stored in the flyweight).

## Intent

> Use sharing to support large numbers of fine-grained objects efficiently. Separate the intrinsic (shared) state from the extrinsic (context-specific) state.

## Problem

When an application must create millions of similar objects (game particles, text characters, map tiles), each object holding its own copy of common data wastes enormous amounts of memory. Even with modern RAM sizes, this can cause GC pressure, cache misses, and degraded performance.

### Bad Example

```csharp
class ParticleBad
{
    // Intrinsic — same for all fire particles: wasted memory for each instance
    public string   Color      { get; init; } = "Orange";
    public byte[]   Texture    { get; init; } = new byte[65_536]; // 64 KB per particle
    public string   Type       { get; init; } = "Fire";

    // Extrinsic — unique per particle
    public float    X          { get; set; }
    public float    Y          { get; set; }
    public float    Velocity   { get; set; }
}

// 10,000 fire particles × 64 KB = 640 MB just for texture data
var particles = Enumerable.Range(0, 10_000)
    .Select(_ => new ParticleBad { X = 0, Y = 0, Velocity = 1.0f })
    .ToList();
```

### Good Example

```csharp
// ── Flyweight — shared intrinsic state ───────────────────
class ParticleType
{
    public string Name    { get; }
    public string Color   { get; }
    public string Texture { get; }   // Simulated as string; in a game this is a GPU texture

    private ParticleType(string name, string color, string texture)
    {
        Name    = name;
        Color   = color;
        Texture = texture;
        Console.WriteLine($"  [Flyweight] Created ParticleType '{name}' (shared)");
    }

    // Flyweight Factory — returns existing instance or creates one
    private static readonly Dictionary<string, ParticleType> _pool = new();

    public static ParticleType Get(string name, string color, string texture)
    {
        if (!_pool.TryGetValue(name, out var type))
        {
            type = new ParticleType(name, color, texture);
            _pool[name] = type;
        }
        return type;
    }

    public static int PoolSize => _pool.Count;
}

// ── Context — extrinsic (unique per particle) ─────────────
struct Particle
{
    public ParticleType Type;      // reference to shared flyweight
    public float        X, Y;
    public float        Velocity;

    public void Draw() =>
        Console.WriteLine($"  [{Type.Name}] @({X:F1},{Y:F1}) v={Velocity:F1} color={Type.Color}");
}

// ── Demo ─────────────────────────────────────────────────
Console.WriteLine("=== Flyweight Pattern ===\n");

// Despite creating 10,000 particles, only 3 ParticleType objects are created
var fireType  = ParticleType.Get("Fire",  "Orange", "fire_tex.png");
var waterType = ParticleType.Get("Water", "Blue",   "water_tex.png");
var smokeType = ParticleType.Get("Smoke", "Gray",   "smoke_tex.png");
ParticleType.Get("Fire",  "Orange", "fire_tex.png");  // returns existing instance

Console.WriteLine($"\nFlyweight pool size: {ParticleType.PoolSize} (regardless of particle count)\n");

var rng = new Random(42);
var particles = Enumerable.Range(0, 10)
    .Select(i => new Particle
    {
        Type     = (i % 3) switch { 0 => fireType, 1 => waterType, _ => smokeType },
        X        = (float)(rng.NextDouble() * 100),
        Y        = (float)(rng.NextDouble() * 100),
        Velocity = (float)(rng.NextDouble() * 5)
    })
    .ToArray();

Console.WriteLine("Drawing particles:");
foreach (var p in particles) p.Draw();

Console.WriteLine($"\n{particles.Length} particles share {ParticleType.PoolSize} ParticleType objects.");
