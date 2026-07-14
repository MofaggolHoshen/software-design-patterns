// ============================================================
// Leader and Followers — C# Simulation
// ============================================================
//
// Intent: Designate exactly one node as the leader that
// serialises all writes and replicates them to followers,
// producing a single consistent order of operations across
// the replica set.
//
// Key roles:
//   Replica      — base node with local log and KV store
//   LeaderNode   — serialises writes, replicates to followers
//   FollowerNode — applies replicated entries, redirects writes
// ============================================================

record Entry(int Index, string Key, string Value);

class Replica(string id)
{
    public string Id { get; } = id;
    public bool Alive { get; set; } = true;

    protected readonly List<Entry> _log = [];
    protected readonly Dictionary<string, string> _store = new();

    // Apply a confirmed entry (called by leader and followers)
    public virtual void Apply(Entry entry)
    {
        _log.Add(entry);
        _store[entry.Key] = entry.Value;
        Console.WriteLine($"  [{Id}] Applied idx={entry.Index}  {entry.Key}='{entry.Value}'");
    }

    public string? Read(string key) =>
        _store.TryGetValue(key, out var v) ? v : null;

    public int LogLength => _log.Count;
}

class FollowerNode(string id) : Replica(id)
{
    private string? _leaderHint;

    public void SetLeaderHint(string leaderId) => _leaderHint = leaderId;

    // Followers redirect writes to the leader
    public void ClientWrite(string key, string value) =>
        Console.WriteLine($"  [{Id}] Redirecting write to leader {_leaderHint}. " +
                          $"(client should retry against leader)");
}

class LeaderNode(string id) : Replica(id)
{
    private readonly List<FollowerNode> _followers = [];
    private int _nextIndex = 0;

    public void AddFollower(FollowerNode f) { _followers.Add(f); f.SetLeaderHint(Id); }

    // Serialise write → replicate to quorum → commit
    public bool Write(string key, string value, int requiredAcks = 0)
    {
        if (!Alive) return false;

        var entry = new Entry(_nextIndex++, key, value);

        // 1. Apply locally (leader's "append to log")
        Apply(entry);

        // 2. Replicate to followers (in practice: parallel async RPCs)
        int acks = 1; // count self
        foreach (var f in _followers)
        {
            if (!f.Alive) continue;
            f.Apply(entry);
            acks++;
        }

        int quorum = requiredAcks > 0 ? requiredAcks : _followers.Count + 1;
        Console.WriteLine($"  [{Id}] Replicated to {acks} node(s).");
        return true;
    }

    // Simulate failover: elect the most up-to-date follower as new leader
    public LeaderNode? Failover()
    {
        Alive = false;
        Console.WriteLine($"\n  *** {Id} (leader) has FAILED ***\n");

        var successor = _followers
            .Where(f => f.Alive)
            .MaxBy(f => f.LogLength);

        if (successor is null) return null;

        var newLeader = new LeaderNode(successor.Id);
        Console.WriteLine($"  New leader elected: {newLeader.Id}");
        return newLeader;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Leader and Followers ===\n");

var leader = new LeaderNode("leader");
var follA = new FollowerNode("follower-A");
var follB = new FollowerNode("follower-B");

leader.AddFollower(follA);
leader.AddFollower(follB);

Console.WriteLine("--- Normal writes via leader ---");
leader.Write("config/timeout", "30s");
leader.Write("config/replicas", "3");
leader.Write("feature/dark-mode", "true");

Console.WriteLine($"\nLog length: leader={leader.LogLength}, A={follA.LogLength}, B={follB.LogLength}");

Console.WriteLine("\n--- Follower redirects a write ---");
follA.ClientWrite("config/timeout", "60s");

Console.WriteLine("\n--- Read from follower (eventual consistency) ---");
Console.WriteLine($"  follower-A sees config/timeout = '{follA.Read("config/timeout")}'");

Console.WriteLine("\n--- Leader failure and failover ---");
var newLeader = leader.Failover();
if (newLeader is not null)
{
    newLeader.Write("config/alert", "leader-changed");
    Console.WriteLine($"  New leader log length: {newLeader.LogLength}");
}
