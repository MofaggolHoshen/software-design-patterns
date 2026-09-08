# 🗳️ Emergent Leader

Many distributed algorithms require a single coordinator (leader) to make progress. The **Emergent Leader** pattern lets nodes automatically elect a leader through a voting algorithm rather than relying on manual configuration or a static primary, making the cluster self-healing when the current leader fails.

## Intent

> Allow nodes to autonomously elect a leader through a voting process, so that a coordinator is always available without any manual intervention.

## Problem

If the leader is statically assigned, a crash requires an operator to reconfigure the cluster. Hardcoding a primary's address couples every node to a specific machine, makes rolling upgrades painful, and means a single crash can halt the entire cluster until someone intervenes.

### Bad Example

```csharp
// Hard-coded leader — any change requires a config update and redeploy
class Node(string id)
{
    private const string HardCodedLeader = "node-1";

    public bool IsLeader() => id == HardCodedLeader;

    public void SubmitWork(string task)
    {
        if (!IsLeader())
            throw new InvalidOperationException($"Not leader. Send to {HardCodedLeader}.");
        Console.WriteLine($"[{id}] Processing: {task}");
    }
}
```

### Good Example

```csharp
// ── Simplified Bully-style election (highest id wins) ──────
class ElectionNode(string id, IReadOnlyList<string> peers)
{
    private string? _currentLeader;
    private readonly Random _rng = new();

    public string? Leader => _currentLeader;

    // Start an election: propose self, collect votes, highest id wins
    public string RunElection()
    {
        var candidates = peers.Append(id).ToList();

        // Each node "votes" — simulate network round: collect alive nodes
        var alive = candidates.Where(p => IsReachable(p)).ToList();

        _currentLeader = alive.Max();   // highest id wins

        Console.WriteLine($"[{id}] Election result: leader = {_currentLeader}");
        return _currentLeader!;
    }

    private bool IsReachable(string peer) =>
        // Simulate occasional node failure
        peer != "node-3" || _rng.NextDouble() > 0.3;

    public void SubmitWork(string task)
    {
        if (id != _currentLeader)
            throw new InvalidOperationException($"Not leader. Forward to {_currentLeader}.");
        Console.WriteLine($"[{id}] (leader) processing: {task}");
    }
}
```

## Key Takeaways

- Real consensus protocols (**Raft**, **Paxos**, **PBFT**) implement emergent leader election as a first-class feature.
- Raft uses randomised election timeouts: the first node to time out requests votes, and the candidate that collects a quorum majority wins.
- A **term** (epoch) number prevents outdated leaders from interfering with a new term's leader.
- The pattern is self-healing: if the leader crashes, a new election starts automatically within the election timeout.

## When to Use

- Any cluster that needs a single coordinator but must survive leader failures without operator intervention.
- Implementing Raft/Paxos replicated state machines.
- Service mesh control planes, key/value stores, distributed locks.

## When NOT to Use

- Single-node services — election adds unnecessary complexity.
- Clusters so small (2 nodes) that a quorum can never be achieved after one failure.
- When external orchestration (Kubernetes, a managed cloud service) already handles leader election for you.
