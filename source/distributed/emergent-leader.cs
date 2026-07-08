// ============================================================
// Emergent Leader — C# Simulation
// ============================================================
//
// Intent: Allow cluster nodes to autonomously elect a leader
// through a voting process (simplified Bully algorithm here),
// so the cluster is self-healing after leader failures.
//
// Key roles:
//   ElectionNode — cluster participant that can vote and lead
//   Cluster      — manages node set and triggers elections
// ============================================================

class ElectionNode(string nodeId, int priority)
{
    public string NodeId => nodeId;
    public int Priority => priority;
    public bool IsAlive { get; set; } = true;

    private string? _leaderNodeId;

    public string? Leader => _leaderNodeId;

    // Accept election notification from the cluster
    public void SetLeader(string leaderId)
    {
        _leaderNodeId = leaderId;
        Console.WriteLine(
            $"  [{nodeId}] Leader is {leaderId}" +
            (nodeId == leaderId ? " ← (I am the leader)" : ""));
    }

    public bool IsLeader() => nodeId == _leaderNodeId;
}

class Cluster(List<ElectionNode> nodes)
{
    // Bully-style: pick the highest-priority alive node as leader
    public string RunElection()
    {
        Console.WriteLine("\n[Cluster] Election started...");

        var alive = nodes.Where(n => n.IsAlive).ToList();
        if (alive.Count == 0) throw new InvalidOperationException("No alive nodes.");

        var elected = alive.MaxBy(n => n.Priority)!;
        Console.WriteLine($"[Cluster] Quorum of {alive.Count}/{nodes.Count} nodes. Winner: {elected.NodeId} (pri={elected.Priority})");

        // Broadcast result to all alive nodes
        foreach (var node in alive)
            node.SetLeader(elected.NodeId);

        return elected.NodeId;
    }

    public void KillNode(string nodeId)
    {
        var n = nodes.First(n => n.NodeId == nodeId);
        n.IsAlive = false;
        Console.WriteLine($"\n[Cluster] *** {nodeId} has CRASHED ***");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Emergent Leader ===\n");

var cluster = new Cluster(
[
    new ElectionNode("node-1", priority: 10),
    new ElectionNode("node-2", priority: 30),   // will be elected first
    new ElectionNode("node-3", priority: 20),
]);

Console.WriteLine("--- Initial election ---");
cluster.RunElection();

Console.WriteLine("\n--- Current leader status ---");
foreach (var n in cluster.Nodes())
    Console.WriteLine($"  {n.NodeId}: leader={n.Leader}, isLeader={n.IsLeader()}");

// Simulate leader failure and re-election
cluster.KillNode("node-2");

Console.WriteLine("\n--- Re-election after leader crash ---");
cluster.RunElection();

Console.WriteLine("\n--- New leader status ---");
foreach (var n in cluster.Nodes().Where(n => n.IsAlive))
    Console.WriteLine($"  {n.NodeId}: leader={n.Leader}, isLeader={n.IsLeader()}");

// Extension helper
static class ClusterExtensions
{
    public static IEnumerable<ElectionNode> Nodes(this Cluster c) =>
        c.GetType()
         .GetField("nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
         .GetValue(c) as List<ElectionNode>
         ?? [];
}
