# 💓 HeartBeat

In a distributed system there is no shared clock and no way to distinguish a crashed node from a slow one. The **HeartBeat** pattern has each node send periodic "I am alive" messages; the absence of a heartbeat within a defined interval is treated as evidence of failure, allowing the cluster to take corrective action automatically.

## Intent

> Have each node broadcast periodic liveness signals so that other nodes can detect failures without polling and trigger recovery — leader failover, partition rebalance, or service deregistration — within a bounded time.

## Problem

Without heartbeats, a node does not know whether a peer is dead or simply slow. Polling every peer on every operation is expensive. Without a bounded detection time, a crashed leader could leave the cluster leaderless for an arbitrarily long time.

### Bad Example

```csharp
// No heartbeat — caller must poll before every operation; no failure detection
class SlowNode(string id)
{
    public bool IsAlive() => true;    // synchronous check; what if it hangs?
}

class Coordinator(IReadOnlyList<SlowNode> nodes)
{
    public SlowNode? GetHealthy() =>
        nodes.FirstOrDefault(n => n.IsAlive());
        // Hangs if a node is slow; no proactive failure detection
}
```

### Good Example

```csharp
class HeartbeatNode(string id, TimeSpan interval)
{
    public string Id => id;

    // Each node runs a background loop that sends heartbeats
    public async Task BroadcastAsync(FailureDetector detector, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            detector.RecordHeartbeat(id, DateTimeOffset.UtcNow);
            await Task.Delay(interval, ct);
        }
    }
}

class FailureDetector(TimeSpan timeout)
{
    private readonly Dictionary<string, DateTimeOffset> _lastSeen = new();

    public void RecordHeartbeat(string nodeId, DateTimeOffset at)
    {
        _lastSeen[nodeId] = at;
        Console.WriteLine($"  [HB] {nodeId} alive at {at:T}");
    }

    public IEnumerable<string> DetectFailures()
    {
        var threshold = DateTimeOffset.UtcNow - timeout;
        return _lastSeen
            .Where(kv => kv.Value < threshold)
            .Select(kv => kv.Key);
    }
}
```

## Key Takeaways

- **Heartbeat interval** and **failure timeout** are the two tuning knobs:
  - Shorter interval → faster detection, more network traffic.
  - Longer timeout → fewer false positives (GC pauses, slow networks), slower recovery.
- Kubernetes uses liveness/readiness probes as an application-level heartbeat mechanism.
- Raft uses **AppendEntries** RPCs (even empty ones) as implicit heartbeats from the leader to followers.
- ZooKeeper session expiry is also driven by heartbeat absence.
- The **Phi Accrual Failure Detector** (Cassandra) uses heartbeat history to output a suspicion level rather than a binary alive/dead decision.

## When to Use

- Any cluster where nodes must detect peer failures within a bounded time to trigger recovery.
- Service registries (Consul, Eureka) that deregister unhealthy instances.
- Leader/follower replication where followers must notice leader absence and start an election.

## When NOT to Use

- Tightly coupled, same-process components — use standard exception handling instead.
- Serverless or ephemeral workloads where instances come and go too quickly for heartbeat registration to be meaningful.
- When the orchestration layer (Kubernetes) already provides health checks and restarts, and your code doesn't need to react to peer failures directly.
