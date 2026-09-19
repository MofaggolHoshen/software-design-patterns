# 🗳️ Quorum

A single node can crash or return stale data. **Quorum** requires that a majority of nodes (write quorum W) agree before a write completes, and that reads consult a sufficient number of nodes (read quorum R) so that at least one node in every read set overlaps with every write set — guaranteeing that the latest write is always visible.

## Intent

> Require a majority of replicas to acknowledge a write and to participate in a read so that the union of any write set and any read set always overlaps, preventing stale reads even in the presence of node failures.

## Problem

If writes go to one node and reads can come from any node, a failed writer means the data is lost, and a node returning stale data is indistinguishable from an up-to-date one. You need a protocol that tolerates `f` failures in an `N`-node cluster.

### Bad Example

```csharp
// Write to one node, read from one node — no quorum
class SingleNodeStore
{
    private string? _value;

    public void Write(string v) => _value = v;
    public string? Read() => _value;   // stale if this node missed a write
}
```

### Good Example

```csharp
class QuorumCluster(int nodeCount = 3)
{
    // Quorum sizes: W + R > N ensures at least one node seen by both
    private int WriteQuorum => nodeCount / 2 + 1;   // majority
    private int ReadQuorum  => nodeCount / 2 + 1;   // majority

    private readonly Dictionary<int, (string Value, long Version)> _nodes =
        Enumerable.Range(0, nodeCount)
                  .ToDictionary(i => i, _ => ((string)null!, 0L));

    public bool Write(string value)
    {
        long version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int acks = 0;

        foreach (var node in _nodes.Keys)
        {
            // Simulate a node being unavailable
            if (node == 1 && ShouldSimulateFault()) continue;

            _nodes[node] = (value, version);
            acks++;
            if (acks >= WriteQuorum)
            {
                Console.WriteLine($"Write quorum achieved ({acks}/{nodeCount})");
                return true;
            }
        }
        return false;   // could not reach write quorum
    }

    public string? Read()
    {
        var responses = new List<(string Value, long Version)>();

        foreach (var node in _nodes.Keys.Take(ReadQuorum))
            responses.Add(_nodes[node]);

        // Return the value with the highest version
        return responses.MaxBy(r => r.Version).Value;
    }

    private static bool ShouldSimulateFault() => Random.Shared.NextDouble() < 0.3;
}
```

## Key Takeaways

- The **quorum rule**: `W + R > N` ensures every read overlaps with every write.
- A 3-node cluster tolerates 1 failure with `W=2, R=2`.
- A 5-node cluster tolerates 2 failures with `W=3, R=3`.
- For write-heavy workloads: lower W (`W=1, R=N`) — fast writes, slow reads.
- For read-heavy workloads: lower R (`W=N, R=1`) — slow writes, fast reads.
- Raft and Paxos use implicit quorums; Dynamo-style systems (Cassandra, Riak) make W and R tunable.

## When to Use

- Any replicated data store where you need to balance availability and consistency under node failures.
- Choosing replication factors and quorum sizes for Cassandra, DynamoDB, or Riak.
- Implementing a custom replicated journal or key/value store.

## When NOT to Use

- Single-node systems — quorum adds roundtrip overhead with no benefit.
- When strong consistency is required: even with quorum, you may need leader-based writes to avoid the "sloppy quorum" pitfall.
- When `N=2` — a majority quorum requires both nodes, providing no fault tolerance.
