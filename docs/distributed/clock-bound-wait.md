# ⏱️ Clock-Bound Wait

In distributed systems, every node has an independent hardware clock that drifts relative to the others. Even with NTP, skew of tens of milliseconds is common. The **Clock-Bound Wait** pattern ensures that a node pauses for the maximum clock-uncertainty window before considering a timestamped write visible to all other nodes, preventing silent causal reordering.

## Intent

> Wait until the uncertainty window (epsilon) around the current wall-clock time has elapsed before committing a timestamped operation, ensuring causal ordering is preserved across nodes.

## Problem

Two nodes each stamp records with `DateTimeOffset.UtcNow`. Node A writes `record_1` at `T = 100 ms`. Node B's clock runs 15 ms behind, so it writes `record_2` at `T = 97 ms`. A reader comparing timestamps will incorrectly treat `record_2` as the earlier event, silently reordering history and breaking invariants that depend on causality.

### Bad Example

```csharp
// No wait — stamp and commit immediately; clock skew can reorder writes
class DataNode(string nodeId)
{
    private readonly List<(long Ts, string Value)> _log = [];

    public void Write(string value)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _log.Add((ts, value));
        // Record visible immediately — but clocks diverge across nodes
        Console.WriteLine($"[{nodeId}] Wrote '{value}' at T={ts} (no wait)");
    }
}
```

### Good Example

```csharp
// Wait for the uncertainty window (epsilon) before the write is considered committed
class ClockBoundNode(string nodeId, int epsilonMs = 7)
{
    private readonly List<(long Ts, string Value)> _log = [];

    public async Task WriteAsync(string value)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Any node that started a write before 'ts' will have committed by now,
        // because even the slowest clock will have advanced past 'ts - epsilonMs'.
        await Task.Delay(epsilonMs);

        _log.Add((ts, value));
        Console.WriteLine($"[{nodeId}] Committed '{value}' at T={ts} (waited {epsilonMs} ms)");
    }

    public IReadOnlyList<(long Ts, string Value)> ReadOrdered() =>
        [.. _log.OrderBy(e => e.Ts)];
}
```

## Key Takeaways

- The uncertainty window **epsilon** equals the worst-case clock skew between any two nodes in the cluster.
- Google Spanner uses **TrueTime** (`[earliest, latest]` intervals) to bound epsilon; CockroachDB uses **Hybrid Logical Clocks** (HLC).
- The wait happens only at commit time; reads remain fast.
- HLC combines a physical clock with a logical counter to shrink epsilon while preserving causality.
- Smaller epsilon → lower latency; larger epsilon → more tolerance for clock drift.

## When to Use

- Multi-region databases needing consistent snapshots ordered by wall-clock time.
- Any system assigning globally comparable timestamps to events where causal ordering matters.
- Implementing serializable snapshot isolation across distributed replicas.

## When NOT to Use

- Inside a single process — logical (Lamport) clocks are sufficient and have zero overhead.
- When event ordering within a node is all that matters.
- If you can tolerate out-of-order delivery (e.g., analytics event streams where eventual ordering is fine).
