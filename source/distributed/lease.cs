// ============================================================
// Lease — C# Simulation
// ============================================================
//
// Intent: Grant a node time-limited, exclusive ownership of a
// resource. If the holder crashes and never renews, the lease
// expires automatically so another node can safely take over.
//
// Key roles:
//   LeaseManager — authoritative store for all active leases
//   LeaseHolder  — a node that acquires and renewsa lease
// ============================================================

record Lease(string Resource, string OwnerId, DateTimeOffset ExpiresAt)
{
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsOwnedBy(string id) => OwnerId == id && !IsExpired;
}

class LeaseManager(TimeSpan ttl)
{
    private readonly Dictionary<string, Lease> _leases = new();
    private readonly object _lock = new();

    // Returns a new Lease if the resource is free (or the old lease expired)
    public bool TryAcquire(string resource, string ownerId, out Lease? lease)
    {
        lock (_lock)
        {
            if (_leases.TryGetValue(resource, out var existing) && !existing.IsExpired
                && existing.OwnerId != ownerId)
            {
                Console.WriteLine($"  [Lease] '{resource}' already held by {existing.OwnerId}" +
                                  $" (expires {existing.ExpiresAt:T})");
                lease = null;
                return false;
            }

            lease = new Lease(resource, ownerId, DateTimeOffset.UtcNow + ttl);
            _leases[resource] = lease;
            Console.WriteLine($"  [Lease] Granted '{resource}' → {ownerId}" +
                              $" (expires {lease.ExpiresAt:T})");
            return true;
        }
    }

    // Holder renews the lease before it expires
    public bool TryRenew(string resource, string ownerId, out Lease? renewed)
    {
        lock (_lock)
        {
            if (!_leases.TryGetValue(resource, out var l) || !l.IsOwnedBy(ownerId))
            {
                renewed = null;
                return false;
            }
            renewed = l with { ExpiresAt = DateTimeOffset.UtcNow + ttl };
            _leases[resource] = renewed;
            Console.WriteLine($"  [Lease] Renewed  '{resource}' for {ownerId}" +
                              $" (new expiry {renewed.ExpiresAt:T})");
            return true;
        }
    }

    public Lease? Get(string resource) =>
        _leases.TryGetValue(resource, out var l) ? l : null;
}

// ── Node that holds and periodically renews a lease ────
class LeaseHolder(string id, LeaseManager manager, TimeSpan renewalInterval)
{
    private Lease? _lease;
    private CancellationTokenSource? _cts;

    public bool IsLeader => _lease?.IsOwnedBy(id) ?? false;

    public bool TryBecomeLeader(string resource)
    {
        if (!manager.TryAcquire(resource, id, out _lease)) return false;
        _cts = new CancellationTokenSource();
        _ = RenewLoopAsync(resource, _cts.Token);
        return true;
    }

    public void Resign() => _cts?.Cancel();

    private async Task RenewLoopAsync(string resource, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(renewalInterval, ct).ConfigureAwait(false);
            if (!manager.TryRenew(resource, id, out _lease))
            {
                Console.WriteLine($"  [{id}] Renewal failed — no longer leader.");
                break;
            }
        }
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Lease Pattern ===\n");

var mgr = new LeaseManager(ttl: TimeSpan.FromSeconds(2));

var holderA = new LeaseHolder("node-A", mgr, renewalInterval: TimeSpan.FromMilliseconds(700));
var holderB = new LeaseHolder("node-B", mgr, renewalInterval: TimeSpan.FromMilliseconds(700));

Console.WriteLine("--- node-A acquires leader lease ---");
holderA.TryBecomeLeader("leader");

Console.WriteLine("\n--- node-B tries to acquire (should fail) ---");
holderB.TryBecomeLeader("leader");

await Task.Delay(800);  // wait one renewal cycle
Console.WriteLine($"\n--- After 800ms: node-A isLeader={holderA.IsLeader} ---");

Console.WriteLine("\n--- node-A resigns (simulates crash) ---");
holderA.Resign();
await Task.Delay(2500); // wait for TTL to expire

Console.WriteLine("\n--- Lease expired; node-B tries again ---");
bool won = holderB.TryBecomeLeader("leader");
Console.WriteLine($"node-B isLeader={won}");
