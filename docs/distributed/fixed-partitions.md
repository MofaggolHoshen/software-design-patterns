# 🗂️ Fixed Partitions

When data must be distributed across multiple nodes, **Fixed Partitions** divides the key space into a predetermined number of partitions (shards). Nodes own subsets of partitions, and when nodes join or leave the cluster only partitions, not individual keys, are reassigned — making rebalancing predictable and efficient.

## Intent

> Divide the data key space into a fixed number of logical partitions, and map partitions to physical nodes — so that rebalancing only moves partitions, not individual records.

## Problem

If you map data directly to nodes (e.g., `node = hash(key) % nodeCount`), every time a node is added or removed the mapping formula changes and almost every key must migrate. At scale this causes massive data movement and temporary unavailability.

### Bad Example

```csharp
// Direct hash-to-node mapping — adding/removing nodes reshuffles everything
class NaiveCluster(int nodeCount)
{
    public int GetNode(string key) =>
        Math.Abs(key.GetHashCode()) % nodeCount;

    // When nodeCount changes from 3 → 4, ~75% of keys move to a different node
}
```

### Good Example

```csharp
// ── Fixed partition ring ──────────────────────────────────
class FixedPartitionCluster
{
    private const int TotalPartitions = 1024;

    // Maps partition → node
    private readonly Dictionary<int, string> _partitionMap = new();

    public void AssignPartitions(IReadOnlyList<string> nodes)
    {
        // Evenly distribute partitions across nodes
        for (int p = 0; p < TotalPartitions; p++)
            _partitionMap[p] = nodes[p % nodes.Count];

        Console.WriteLine($"Assigned {TotalPartitions} partitions across {nodes.Count} nodes.");
    }

    public string GetNode(string key)
    {
        int partition = Math.Abs(key.GetHashCode()) % TotalPartitions;
        return _partitionMap[partition];
    }

    public void Rebalance(IReadOnlyList<string> nodes)
    {
        // When nodes change, only ~(1/nodeCount) of partitions move
        int moved = 0;
        for (int p = 0; p < TotalPartitions; p++)
        {
            string newNode = nodes[p % nodes.Count];
            if (_partitionMap[p] != newNode) { _partitionMap[p] = newNode; moved++; }
        }
        Console.WriteLine($"Rebalance: {moved}/{TotalPartitions} partitions reassigned.");
    }
}
```

## Key Takeaways

- The number of partitions is typically much larger than the number of nodes (e.g., Kafka: 100 partitions, 3 brokers; Redis Cluster: 16 384 slots).
- A **partition map** (partition → node) is stored in the consistent core; clients fetch it on startup and cache it.
- Only partition boundaries move on rebalance; individual records stay in place within their partition.
- Cassandra and Riak use **consistent hashing** with virtual nodes — a related technique that also avoids global reshuffling.

## When to Use

- Horizontally scalable key/value stores, databases, or message queues.
- Any system where nodes join and leave and you need bounded rebalancing cost.
- When you need predictable, even data distribution across nodes.

## When NOT to Use

- Very small datasets that fit on one node — partitioning adds unnecessary complexity.
- Workloads with extreme hotspots where a fixed hash-based partition will always hit the same shard.
- When your cloud provider's managed service (DynamoDB, Cosmos DB) already handles partitioning internally.
