# 🧮 Version Vector

In a single-master system, a monotonically increasing version number is sufficient to determine which write is newer. In a **multi-master** or **leaderless** replication system, two nodes can independently accept writes to the same key. A **Version Vector** (also known as a Vector Clock variant) assigns each node its own counter, so the system can determine whether two writes are causally ordered or genuinely concurrent — enabling conflict detection rather than silently discarding data.

## Intent

> Maintain a per-node version counter at each replica; compare vectors to determine whether one value causally follows another or whether two values are concurrent and require explicit conflict resolution.

## Problem

Using a single integer version number in a multi-master store is insufficient: Node A increments the counter to `v=2` and Node B independently increments it also to `v=2` from the same base. Comparing `v=2` vs `v=2` gives no indication that both writes happened concurrently and that data may be lost by taking either one.

### Bad Example

```csharp
// Single version number — concurrent writes appear identical, data is silently lost
class SingleVersionStore
{
    private string? _value;
    private int _version;

    public void Write(string val, int clientVersion)
    {
        if (clientVersion < _version)
        {
            Console.WriteLine("Rejected: stale write.");
            return;
        }
        _value   = val;
        _version = clientVersion + 1;
    }
    // Two nodes both at v=1 both write → last one wins silently
}
```

### Good Example

```csharp
using VV = Dictionary<string, int>;

static class VVExtensions
{
    // A dominates B if every counter in A >= B and at least one is greater
    public static bool Dominates(this VV a, VV b) =>
        b.All(kv => a.GetValueOrDefault(kv.Key) >= kv.Value) &&
        a.Any(kv => kv.Value > b.GetValueOrDefault(kv.Key));

    // Concurrent: neither dominates the other
    public static bool IsConcurrentWith(this VV a, VV b) =>
        !a.Dominates(b) && !b.Dominates(a);

    public static VV Increment(this VV vv, string nodeId)
    {
        var next = new VV(vv);
        next[nodeId] = next.GetValueOrDefault(nodeId) + 1;
        return next;
    }
    public static string Format(this VV vv) =>
        "{" + string.Join(", ", vv.Select(kv => $"{kv.Key}:{kv.Value}")) + "}";
}

class VersionedValue(string nodeId)
{
    public string?  Value   { get; private set; }
    public VV       Version { get; private set; } = new();

    public void Write(string value)
    {
        Version = Version.Increment(nodeId);
        Value   = value;
        Console.WriteLine($"  [{nodeId}] Write '{value}' @ {Version.Format()}");
    }

    // Returns true if this write should be kept alongside other (conflict)
    public (bool IsConflict, string? Accepted) Merge(VersionedValue other)
    {
        if (Version.Dominates(other.Version))
        {
            Console.WriteLine($"  [{nodeId}] Accepted own version (other is stale).");
            return (false, Value);
        }
        if (other.Version.Dominates(Version))
        {
            Console.WriteLine($"  [{nodeId}] Accepted other's version (ours is stale).");
            return (false, other.Value);
        }
        Console.WriteLine($"  [{nodeId}] CONFLICT — concurrent writes, both kept for resolution.");
        return (true, null);
    }
}
```

## Key Takeaways

- Two version vectors are **comparable** (one happened-before the other) or **concurrent** (neither dominates).
- Concurrent values are presented to the application to resolve — they are NOT silently overwritten.
- **Riak** stores sibling values on conflict; **DynamoDB** uses a simpler last-writer-wins per table.
- Amazon's Dynamo paper introduced version vectors to e-commerce shopping carts to prevent silent cart data loss.
- Version vectors grow one entry per node; in large clusters use **dotted version vectors** to bound size.

## When to Use

- Leaderless or multi-master replication where any node can accept writes (Riak, CRDTs, mobile sync).
- Collaborative editing or offline-first applications where clients make independent changes.
- Any system where "last write wins" would silently discard legitimate concurrent mutations.

## When NOT to Use

- Single-leader replication — a simple monotonic log index is sufficient.
- When conflict resolution is impossible and you must prevent concurrent writes (use a lease or distributed lock instead).
- If you need strict serializability — use a consensus protocol rather than optimistic replication with conflict detection.
