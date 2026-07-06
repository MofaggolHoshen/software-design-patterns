// ============================================================
// Consistent Core — C# Simulation
// ============================================================
//
// Intent: Keep a small (3–5 node) strongly consistent cluster
// that the rest of the system relies on for coordination —
// leader election, locks, config — without requiring every
// data node to participate in consensus.
//
// Key roles:
//   ConsistentCore — simulated Raft log (always consistent)
//   DataNode       — large-scale node that uses the core
// ============================================================

// ── Consistent Core (simulates a Raft-backed KV store) ────
record LogEntry(int Index, string Key, string Value);

class ConsistentCore
{
    private readonly List<LogEntry> _log = [];
    private readonly Dictionary<string, string> _store = new();
    private int _commitIndex = -1;

    // In a real system this goes through Raft: propose → replicate → commit
    // Here we simulate a quorum write that always succeeds
    public bool Write(string key, string value)
    {
        int idx = _log.Count;
        _log.Add(new LogEntry(idx, key, value));
        CommitUpTo(idx);
        Console.WriteLine($"  [Core] Written  {key} = '{value}'  (logIdx={idx})");
        return true;
    }

    public string? Read(string key) =>
        _store.TryGetValue(key, out var v) ? v : null;

    public IEnumerable<string> ListPrefix(string prefix) =>
        _store.Keys.Where(k => k.StartsWith(prefix));

    private void CommitUpTo(int idx)
    {
        for (int i = _commitIndex + 1; i <= idx; i++)
            _store[_log[i].Key] = _log[i].Value;
        _commitIndex = idx;
    }
}

// ── Data nodes delegate all coordination to the core ──────
class DataNode(string id, ConsistentCore core)
{
    public string Id => id;

    // Register presence in service registry
    public void Register() =>
        core.Write($"nodes/{id}/status", "alive");

    // Read leader assignment from core — guaranteed consistent
    public string? GetLeader() =>
        core.Read("election/leader");

    // Acquire a distributed lock (simplified — no TTL here)
    public bool AcquireLock(string resource)
    {
        if (core.Read($"locks/{resource}") is not null)
        {
            Console.WriteLine($"  [{id}] Lock '{resource}' already held.");
            return false;
        }
        core.Write($"locks/{resource}", id);
        Console.WriteLine($"  [{id}] Lock '{resource}' acquired.");
        return true;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Consistent Core ===\n");

var core = new ConsistentCore();
var nodeA = new DataNode("node-a", core);
var nodeB = new DataNode("node-b", core);
var nodeC = new DataNode("node-c", core);

Console.WriteLine("--- Service registration ---");
nodeA.Register();
nodeB.Register();
nodeC.Register();

Console.WriteLine("\n--- Leader election (stored in core) ---");
core.Write("election/leader", "node-a");
Console.WriteLine($"  node-b sees leader: {nodeB.GetLeader()}");
Console.WriteLine($"  node-c sees leader: {nodeC.GetLeader()}");

Console.WriteLine("\n--- Distributed locking ---");
nodeA.AcquireLock("partition-1");
nodeB.AcquireLock("partition-1");  // should fail
nodeB.AcquireLock("partition-2");  // should succeed

Console.WriteLine("\n--- Registered nodes ---");
foreach (var key in core.ListPrefix("nodes/"))
    Console.WriteLine($"  {key} = {core.Read(key)}");
