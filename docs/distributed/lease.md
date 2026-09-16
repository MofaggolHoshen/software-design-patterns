# 🔑 Lease

Distributed systems need exclusive resource ownership (leader status, lock on a record, message ownership) that survives process crashes. A mutex in a shared database holds its lock forever if the holder crashes. The **Lease** pattern grants time-limited ownership that expires automatically, so a crashed holder never prevents other nodes from taking over.

## Intent

> Grant a node exclusive, time-limited ownership of a resource. If the holder does not renew the lease before it expires, ownership automatically lapses so another node can acquire it.

## Problem

A traditional distributed lock (record in DB, Redis SETNX) is held until the holder explicitly releases it. If the holder crashes after acquiring the lock, no one else can acquire it — the cluster stalls until an operator times out the lock manually.

### Bad Example

```csharp
// Lock with no TTL — holder crash = permanent deadlock
class DistributedLock(IDatabase db, string resource)
{
    public async Task<bool> AcquireAsync(string ownerId)
    {
        // SET if not exists — but no expiry
        return await db.StringSetAsync(resource, ownerId, when: When.NotExists);
    }

    public async Task ReleaseAsync(string ownerId)
    {
        if (await db.StringGetAsync(resource) == ownerId)
            await db.KeyDeleteAsync(resource);
        // If holder crashes before Release(), lock is held forever
    }
}
```

### Good Example

```csharp
record Lease(string OwnerId, DateTimeOffset ExpiresAt)
{
    public bool IsValid(string requesterId) =>
        OwnerId == requesterId && DateTimeOffset.UtcNow < ExpiresAt;
}

class LeaseManager
{
    private readonly Dictionary<string, Lease> _leases = new();
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(10);

    // Acquire returns success only if resource is free or lease has expired
    public bool TryAcquire(string resource, string ownerId, out Lease? lease)
    {
        if (_leases.TryGetValue(resource, out var existing) &&
            DateTimeOffset.UtcNow < existing.ExpiresAt &&
            existing.OwnerId != ownerId)
        {
            lease = null;
            return false;     // still held by someone else
        }

        lease = new Lease(ownerId, DateTimeOffset.UtcNow + _ttl);
        _leases[resource] = lease;
        Console.WriteLine($"Lease granted: '{resource}' → {ownerId} until {lease.ExpiresAt:T}");
        return true;
    }

    // Holder must renew before expiry
    public bool TryRenew(string resource, string ownerId)
    {
        if (!_leases.TryGetValue(resource, out var l) || !l.IsValid(ownerId)) return false;
        _leases[resource] = l with { ExpiresAt = DateTimeOffset.UtcNow + _ttl };
        return true;
    }
}
```

## Key Takeaways

- The **TTL** must be long enough that a healthy holder can always renew before expiry (accounting for GC pauses, slow networks).
- Lease holders should renew at `TTL/2` to leave a safety margin.
- Redis `SET key value EX 10 NX` implements a lease atomically in one command.
- **Fencing tokens** (a monotonically increasing version attached to the lease) prevent a zombie holder from making changes after its lease expired.
- etcd leases with keepalive gRPC streams are the canonical cloud-native example.

## When to Use

- Distributed leader election where the leader must heartbeat to retain its role.
- Consumer group partition assignment in Kafka (group sessions are leases).
- Distributed rate limiting, feature flag ownership, or shard ownership.

## When NOT to Use

- Short-lived resources where the overhead of lease negotiation exceeds the operation itself.
- When you can design the protocol to be idempotent and repeat-safe, eliminating the need for mutual exclusion.
- Single-process applications — a plain `lock` or `Mutex` is sufficient.
