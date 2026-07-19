// ============================================================
// Quorum — C# Simulation
// ============================================================
//
// Intent: Require W nodes to acknowledge a write and R nodes
// to respond to a read such that W + R > N, guaranteeing
// that every read intersects with every write set — ensuring
// the latest write is always visible despite node failures.
//
// Key roles:
//   Replica      — individual node storing (value, version)
//   QuorumStore  — coordinates W-of-N writes and R-of-N reads
// ============================================================

record ReplicaState(string? Value, long Version);

class Replica(string id)
{
    public string Id { get; } = id;
    public bool Down { get; set; } = false;
    private ReplicaState _state = new(null, 0);

    public bool TryWrite(string value, long version)
    {
        if (Down) { Console.WriteLine($"  [{id}] ✗ unreachable"); return false; }
        _state = new(value, version);
        Console.WriteLine($"  [{id}] ✓ stored v{version}='{value}'");
        return true;
    }

    public ReplicaState? TryRead()
    {
        if (Down) { Console.WriteLine($"  [{id}] ✗ unreachable"); return null; }
        Console.WriteLine($"  [{id}] ✓ v{_state.Version}='{_state.Value}'");
        return _state;
    }
}

class QuorumStore(int n = 3, int w = 2, int r = 2)
{
    private readonly List<Replica> _replicas =
        Enumerable.Range(1, n).Select(i => new Replica($"node-{i}")).ToList();

    public IReadOnlyList<Replica> Replicas => _replicas;

    public bool Write(string value)
    {
        var version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Console.WriteLine($"\nWrite '{value}' (need W={w}/{n} acks):");
        int acks = _replicas.Count(r => r.TryWrite(value, version));

        bool ok = acks >= w;
        Console.WriteLine(ok
            ? $"  → Write COMMITTED ({acks} acks ≥ W={w})"
            : $"  → Write FAILED    ({acks} acks < W={w})");
        return ok;
    }

    public string? Read()
    {
        Console.WriteLine($"\nRead (need R={r}/{n} responses):");
        var responses = _replicas
            .Select(rep => rep.TryRead())
            .Where(s => s is not null)
            .ToList();

        if (responses.Count < r)
        {
            Console.WriteLine($"  → Read FAILED ({responses.Count} responses < R={r})");
            return null;
        }

        // Return value with the highest version (most recent write)
        var best = responses.MaxBy(s => s!.Version)!;
        Console.WriteLine($"  → Read SUCCEEDED: v{best.Version}='{best.Value}'");
        return best.Value;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Quorum (W=2, R=2, N=3) ===\n");
Console.WriteLine("W + R = 4 > N = 3  → every read overlaps every write\n");

var store = new QuorumStore(n: 3, w: 2, r: 2);

store.Write("config-v1");
store.Read();

Console.WriteLine("\n--- Simulate node-1 going down ---");
store.Replicas[0].Down = true;

store.Write("config-v2");   // still achieves W=2 with node-2 + node-3
store.Read();               // still achieves R=2 with node-2 + node-3

Console.WriteLine("\n--- Simulate node-2 also going down (only 1 alive) ---");
store.Replicas[1].Down = true;

store.Write("config-v3");   // fails: only 1 node, W=2 not reachable
store.Read();               // fails: only 1 node, R=2 not reachable
