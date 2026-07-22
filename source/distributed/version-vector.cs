// ============================================================
// Version Vector — C# Simulation
// ============================================================
//
// Intent: In a multi-master replication system, maintain a
// per-node version counter so the system can determine
// whether two writes are causally ordered (one happened-before
// the other) or genuinely concurrent — enabling conflict
// detection rather than silent data loss.
//
// Key roles:
//   VV             — version vector (Dictionary<nodeId, int>)
//   VersionedValue — value + VV stored at each replica
//   Replica        — node that can write and merge from peers
// ============================================================

using VV = Dictionary<string, int>;

static class VVExtensions
{
    // a happened-before b if every counter a[n] <= b[n]
    public static bool HappenedBefore(this VV a, VV b) =>
        a.All(kv => b.GetValueOrDefault(kv.Key) >= kv.Value) &&
        a.Any(kv => b.GetValueOrDefault(kv.Key) > kv.Value);

    // concurrent: neither dominates the other
    public static bool IsConcurrentWith(this VV a, VV b) =>
        !a.HappenedBefore(b) && !b.HappenedBefore(a) && a.Any();

    public static VV Increment(this VV vv, string nodeId)
    {
        var next = new VV(vv) { [nodeId] = vv.GetValueOrDefault(nodeId) + 1 };
        return next;
    }

    public static VV Merge(this VV a, VV b)
    {
        var merged = new VV(a);
        foreach (var (k, v) in b)
            merged[k] = Math.Max(merged.GetValueOrDefault(k), v);
        return merged;
    }

    public static string Format(this VV vv) =>
        "{" + string.Join(", ", vv.Select(kv => $"{kv.Key}:{kv.Value}")) + "}";
}

record VersionedValue(string NodeId, string? Value, VV Version)
{
    // Returns merged result + whether it was a conflict
    public static (VersionedValue Resolved, bool WasConflict) Merge(
        VersionedValue local, VersionedValue incoming)
    {
        if (local.Version.HappenedBefore(incoming.Version))
        {
            Console.WriteLine($"  [{local.NodeId}] Incoming dominates local — accepted '{incoming.Value}'.");
            return (incoming, false);
        }
        if (incoming.Version.HappenedBefore(local.Version))
        {
            Console.WriteLine($"  [{local.NodeId}] Local dominates incoming — kept '{local.Value}'.");
            return (local, false);
        }
        // Truly concurrent — CONFLICT
        var mergedVV = local.Version.Merge(incoming.Version);
        // Application-level resolution: keep both (Last-Write-Wins shown here for demo)
        Console.WriteLine($"  [{local.NodeId}] ⚡ CONFLICT — local='{local.Value}' " +
                          $"vs incoming='{incoming.Value}'. Keeping both as siblings.");
        return (new VersionedValue(local.NodeId, $"[{local.Value}|{incoming.Value}]", mergedVV), true);
    }
}

class Replica(string id)
{
    private readonly Dictionary<string, VersionedValue> _store = new();
    private VV _clock = new();

    public string Id => id;

    // Local write: increment this node's counter
    public void Write(string key, string value)
    {
        _clock = _clock.Increment(id);
        _store[key] = new VersionedValue(id, value, new VV(_clock));
        Console.WriteLine($"  [{id}] Write '{key}'='{value}' @ {_clock.Format()}");
    }

    public VersionedValue? Get(string key) =>
        _store.TryGetValue(key, out var v) ? v : null;

    // Receive a replicated value from another node
    public void Replicate(VersionedValue incoming)
    {
        if (!_store.TryGetValue(incoming.NodeId + ":merged", out var local))
            local = _store.GetValueOrDefault(incoming.NodeId) ?? incoming with { NodeId = id };

        var (resolved, _) = VersionedValue.Merge(local, incoming);
        _store[incoming.NodeId] = resolved;
        _clock = _clock.Merge(resolved.Version);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Version Vector ===\n");

var replicaA = new Replica("A");
var replicaB = new Replica("B");

Console.WriteLine("--- Node A and B both start from scratch ---\n");
replicaA.Write("cart", "['apple']");
replicaB.Write("cart", "['banana']");   // concurrent with A's write

Console.WriteLine("\n--- Replicating A → B and B → A ---\n");
var aCart = replicaA.Get("cart")!;
var bCart = replicaB.Get("cart")!;

Console.WriteLine("B merges A's write:");
replicaB.Replicate(aCart);

Console.WriteLine("\nA merges B's write:");
replicaA.Replicate(bCart);

Console.WriteLine("\n--- Causally ordered writes ---\n");
var replicaC = new Replica("C");
replicaC.Write("cart", "['cherry']");
replicaC.Write("cart", "['cherry','date']");   // 2nd write causally follows 1st

var firstWrite = new VersionedValue("C", "['cherry']", new VV { ["C"] = 1 });
var secondWrite = new VersionedValue("C", "['cherry','date']", new VV { ["C"] = 2 });

Console.WriteLine($"\nFirst  VV: {firstWrite.Version.Format()}");
Console.WriteLine($"Second VV: {secondWrite.Version.Format()}");
Console.WriteLine($"First happened-before second: {firstWrite.Version.HappenedBefore(secondWrite.Version)}");
Console.WriteLine($"Concurrent: {firstWrite.Version.IsConcurrentWith(secondWrite.Version)}");
