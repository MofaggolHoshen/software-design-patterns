// ============================================================
// Flyweight Pattern — C# Example
// ============================================================
//
// Intent: Use sharing to support large numbers of fine-grained
// objects. Separate intrinsic (shared) from extrinsic (unique) state.
//
// Key roles:
//   TreeType    — Flyweight (shared intrinsic state)
//   TreeFactory — Flyweight Factory (pool/registry)
//   Tree        — Context (extrinsic state + flyweight reference)
// ============================================================

// ── Flyweight — shared intrinsic state ──────────────────
class TreeType
{
    public string Name { get; }
    public string Color { get; }
    public string Texture { get; }   // In a real app: loaded GPU texture (expensive)

    // Private constructor — only TreeFactory creates instances
    private TreeType(string name, string color, string texture)
    {
        Name = name;
        Color = color;
        Texture = texture;
        Console.WriteLine($"  [Flyweight] Created TreeType '{name}' (size: ~64KB simulated)");
    }

    // ── Flyweight Factory ──────────────────────────────
    private static readonly Dictionary<string, TreeType> _pool = new();

    public static TreeType Get(string name, string color, string texture)
    {
        var key = $"{name}|{color}";
        if (!_pool.TryGetValue(key, out var type))
        {
            type = new TreeType(name, color, texture);
            _pool[key] = type;
        }
        return type;
    }

    public static int PoolSize => _pool.Count;

    public void Draw(float x, float y, float scale) =>
        Console.WriteLine($"    Draw {Name,-12} [{Color}] @({x:F0},{y:F0}) scale={scale:F1}x");
}

// ── Context — extrinsic (per-tree) state ─────────────────
// Use a struct to keep memory overhead minimal
struct Tree
{
    public TreeType Flyweight;   // shared — pointer only
    public float X, Y;        // unique per instance
    public float Scale;

    public void Draw() => Flyweight.Draw(X, Y, Scale);
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Flyweight Pattern ===\n");
Console.WriteLine("Setting up shared TreeType objects (flyweights):\n");

// Only 4 TreeType instances will be created, regardless of forest size
var oak = TreeType.Get("Oak", "DarkGreen", "oak_bark.png");
var pine = TreeType.Get("Pine", "Green", "pine_bark.png");
var birch = TreeType.Get("Birch", "White", "birch_bark.png");
var maple = TreeType.Get("Maple", "OrangeRed", "maple_bark.png");
TreeType.Get("Oak", "DarkGreen", "oak_bark.png"); // returns existing instance

Console.WriteLine($"\nFlyweight pool: {TreeType.PoolSize} unique TreeType objects\n");

// Plant 1,000,000 trees — but only 4 TreeType objects exist in memory
var rng = new Random(42);
var types = new[] { oak, pine, birch, maple };
const int N = 1_000_000;

Console.WriteLine($"Planting {N:N0} trees using only {TreeType.PoolSize} shared type objects...\n");

var forest = new Tree[N];
for (int i = 0; i < N; i++)
{
    forest[i] = new Tree
    {
        Flyweight = types[i % types.Length],
        X = (float)(rng.NextDouble() * 10_000),
        Y = (float)(rng.NextDouble() * 10_000),
        Scale = (float)(0.5 + rng.NextDouble() * 2.0)
    };
}

Console.WriteLine("Drawing first 5 trees:");
for (int i = 0; i < 5; i++) forest[i].Draw();

Console.WriteLine($"\nMemory saving: {N:N0} trees, but texture data stored only {TreeType.PoolSize} times.");
Console.WriteLine($"Without flyweight: {N:N0} × 64KB = {N * 64 / 1024:N0} MB");
Console.WriteLine($"With flyweight:    {TreeType.PoolSize} × 64KB = {TreeType.PoolSize * 64} KB");
