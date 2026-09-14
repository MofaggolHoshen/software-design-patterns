# 👑 Leader and Followers

Allowing any node to accept writes in a multi-replica system leads to conflicting updates and split-brain scenarios. **Leader and Followers** designates exactly one node as the leader for each term; all writes go through the leader, which then replicates them to follower nodes before acknowledging the client.

## Intent

> Elect one node as the leader that serialises all writes and propagates them to followers, ensuring a single, consistent order of operations across the replica set while followers serve as hot standbys ready to be promoted.

## Problem

Without a leader, two nodes can concurrently accept writes to the same key with different values. Merging those conflicting writes requires conflict resolution logic that is difficult to make correct for all data types, and the client has no guarantee which value "wins."

### Bad Example

```csharp
// Any node accepts writes — race conditions and split-brain guaranteed
class Replica(string id)
{
    public Dictionary<string, string> Store { get; } = new();

    // Two replicas can accept conflicting writes with no coordination
    public void Write(string key, string value) => Store[key] = value;
}
```

### Good Example

```csharp
class ReplicaNode(string id)
{
    public string Id => id;
    public Dictionary<string, string> Store { get; } = new();

    // Followers only apply replicated entries; they reject direct client writes
    public void Apply(string key, string value)
    {
        Store[key] = value;
        Console.WriteLine($"  [{id}] Applied {key}={value}");
    }
}

class LeaderNode(string id, IReadOnlyList<ReplicaNode> followers) : ReplicaNode(id)
{
    // All client writes go through the leader
    public bool Write(string key, string value, int writeQuorum = 0)
    {
        Apply(key, value);   // leader writes locally first

        int acks = 1;        // count self
        foreach (var follower in followers)
        {
            follower.Apply(key, value);
            acks++;
            if (writeQuorum > 0 && acks >= writeQuorum)
            {
                Console.WriteLine($"[{Id}] Write quorum {acks} reached.");
                break;
            }
        }
        return true;
    }

    // On leader failure a follower runs an election and becomes the new leader
    public ReplicaNode FailoverTo(ReplicaNode newLeader)
    {
        Console.WriteLine($"[Failover] {Id} → {newLeader.Id} becomes new leader.");
        return newLeader;
    }
}
```

## Key Takeaways

- All writes are serialised by the leader, producing a **total order** of operations that every follower applies in the same sequence.
- Followers serve reads (with eventual consistency) when read-your-writes isolation is not required.
- If the leader fails, a new leader is elected via the **Emergent Leader** pattern; recovery requires the new leader to have all committed entries.
- Raft explicitly models this: the leader appends entries, replicates to a majority, then commits — the follower cannot accept writes.
- Primary/Replica databases (PostgreSQL streaming replication, MySQL replication) follow this exact pattern.

## When to Use

- Any replicated store where strong write ordering is required.
- Consensus-based state machines (etcd, ZooKeeper, Raft-backed databases).
- Message broker partitions where a single partition leader serialises all writes.

## When NOT to Use

- Read-heavy workloads where a single leader becomes a write bottleneck — consider partitioned leadership (each partition has its own leader).
- Leaderless writes are preferred (Cassandra, Riak) for maximum write availability at the cost of conflict resolution complexity.
- Scenarios where a single leader would cross geographic boundaries, causing excessive write latency.
