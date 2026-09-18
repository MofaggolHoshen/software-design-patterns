# 🌊 Low-Water Mark

Distributed systems that use a **Write-Ahead Log** (WAL) accumulate entries indefinitely unless the log is truncated. The **Low-Water Mark** (LWM) pattern tracks the minimum log index that every follower has durably applied, allowing entries below that index to be safely discarded and disk space to be reclaimed.

## Intent

> Track the lowest log index that all known replicas have applied, and truncate the log below that index to bound storage growth.

## Problem

A replicated log grows without bound unless entries below the point where all replicas are caught up are removed. Without a coordinated truncation mechanism, the leader cannot safely delete any entry because a lagging follower may still need it for recovery.

### Bad Example

```csharp
// Log grows forever — no truncation mechanism
class ReplicatedLog
{
    private readonly List<string> _entries = [];

    public int Append(string entry)
    {
        _entries.Add(entry);
        return _entries.Count - 1;     // index
    }

    // No way to truncate — disk fills up
    public IReadOnlyList<string> All => _entries;
}
```

### Good Example

```csharp
class ReplicatedLog
{
    private readonly List<(int Index, string Entry)> _log = [];
    // Per-follower: highest index the follower has acknowledged
    private readonly Dictionary<string, int> _followerAck = new();

    public int Append(string entry)
    {
        int index = _log.Count == 0 ? 0 : _log[^1].Index + 1;
        _log.Add((index, entry));
        return index;
    }

    // Follower reports the highest index it has durably applied
    public void Acknowledge(string followerId, int appliedIndex)
    {
        _followerAck[followerId] = appliedIndex;
        TryTruncate();
    }

    private void TryTruncate()
    {
        if (_followerAck.Count == 0) return;

        // Low-water mark = min across all followers
        int lwm = _followerAck.Values.Min();

        int before = _log.Count;
        _log.RemoveAll(e => e.Index < lwm);
        Console.WriteLine($"Truncated below LWM={lwm}: removed {before - _log.Count} entries.");
    }

    public IReadOnlyList<(int Index, string Entry)> Since(int fromIndex) =>
        _log.Where(e => e.Index >= fromIndex).ToList();
}
```

## Key Takeaways

- The LWM is the **minimum** of all follower acknowledgement indices — the leader cannot advance it past any lagging follower.
- Raft calls the equivalent concept the **commit index** paired with **snapshot** compaction.
- In practice, followers that fall too far behind may need to receive a full snapshot rather than replaying from the truncated log.
- The LWM is also used in stream processing (Kafka, Flink) to know when it's safe to emit windowed aggregations.

## When to Use

- Any replicated state machine or WAL-backed database where the log must be bounded in size.
- Stream processing frameworks managing event-time watermarks.
- Message queues where consumers track offsets and the broker needs to know when to expire messages.

## When NOT to Use

- If you never truncate (audit logs, immutable event sourcing) — retain all entries by design.
- When disk is cheap and the log is naturally small — premature truncation adds complexity.
- When any single slow follower would block truncation indefinitely — use a retention policy with catch-up snapshots instead.
