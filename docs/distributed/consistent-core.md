# 🧱 Consistent Core

Implementing strong consistency across an entire large cluster is expensive. The **Consistent Core** pattern solves this by designating a small cluster of nodes (typically 3–5) that use a consensus protocol (Raft, Paxos) to form a strongly consistent store, while the larger set of data nodes delegates coordination tasks to this core.

## Intent

> Maintain a small, strongly consistent cluster that the rest of the system relies on for coordination tasks such as leader election, configuration management, and distributed locking — without requiring every node to participate in consensus.

## Problem

You need distributed coordination (who is the leader? what is the current config?), but running a consensus protocol across hundreds of data nodes is prohibitively expensive. Every write would require a quorum of all nodes to agree, and the protocol's message complexity grows with cluster size.

### Bad Example

```csharp
// Every data node tries to maintain its own consistent state via gossip
// Result: split-brain, conflicting leaders, lost config changes
class DataNode(string id)
{
    private string _leader = "unknown";

    // No consensus — last writer wins; clashing updates cause split-brain
    public void SetLeader(string nodeId) => _leader = nodeId;
    public string GetLeader() => _leader;
}
```

### Good Example

```csharp
// ── Consistent Core (simulated Raft log) ──────────────────
record LogEntry(int Index, string Key, string Value);

class ConsistentCore
{
    private readonly List<LogEntry>           _log      = [];
    private readonly Dictionary<string, string> _store  = new();
    private int _commitIndex = -1;

    // In a real system, this write goes through Raft:
    // leader appends → replicates to majority → commits
    public bool Write(string key, string value)
    {
        int index = _log.Count;
        _log.Add(new LogEntry(index, key, value));
        // Simulate quorum acknowledgement
        Commit(index);
        return true;
    }

    private void Commit(int index)
    {
        for (int i = _commitIndex + 1; i <= index; i++)
            _store[_log[i].Key] = _log[i].Value;
        _commitIndex = index;
    }

    public string? Read(string key) =>
        _store.GetValueOrDefault(key);
}

// ── Data nodes delegate coordination to the core ──────────
class DataNode(string id, ConsistentCore core)
{
    public string GetLeader() => core.Read("leader") ?? "none";
    public void RegisterSelf() => core.Write($"nodes/{id}", "alive");
}
```

## Key Takeaways

- Real-world examples: **ZooKeeper**, **etcd**, **Consul** all act as consistent cores for larger distributed systems.
- The core cluster size is kept small (3 or 5 nodes) to keep consensus overhead manageable.
- Data nodes never need to participate in consensus — they simply read and write to the core.
- The core handles leader election, distributed locks, service discovery, and configuration with strong guarantees.

## When to Use

- You need coordination (leader election, locks, config) with strong consistency guarantees.
- Your data cluster is large and running Raft/Paxos on every node is impractical.
- Using a managed consistent store (etcd, ZooKeeper) as your coordination backbone.

## When NOT to Use

- Your entire cluster is small enough (≤5 nodes) that every node can participate in consensus.
- Coordination is purely eventual; strong consistency is not required.
- The core becomes a single point of failure if you don't replicate it — don't skip replication.
