// ============================================================
// Low-Water Mark — C# Simulation
// ============================================================
//
// Intent: Track the minimum log index acknowledged by all
// followers (the Low-Water Mark) and truncate the write-ahead
// log below that index to bound unbounded storage growth.
//
// Key roles:
//   ReplicatedLog   — WAL with append, acknowledge, and truncate
//   FollowerTracker — per-follower acknowledgement registry
// ============================================================

record LogEntry(int Index, string Key, string Value);

class FollowerTracker
{
    private readonly Dictionary<string, int> _acked = new();

    public void Acknowledge(string followerId, int index)
    {
        _acked[followerId] = index;
        Console.WriteLine($"  [Follower] {followerId} acked up to index {index}");
    }

    // Low-water mark: minimum index ALL known followers have applied
    public int LowWaterMark() =>
        _acked.Count == 0 ? -1 : _acked.Values.Min();

    public int FollowerCount => _acked.Count;
}

class ReplicatedLog(FollowerTracker tracker)
{
    private readonly List<LogEntry> _log = [];
    private int _nextIndex = 0;
    private int _truncatedBelow = 0;

    public int Append(string key, string value)
    {
        int index = _nextIndex++;
        _log.Add(new LogEntry(index, key, value));
        Console.WriteLine($"  [Log] Appended idx={index}  {key}='{value}'   (log size={_log.Count})");
        return index;
    }

    // Called when a follower reports it has applied up to appliedIndex
    public void OnFollowerAck(string followerId, int appliedIndex)
    {
        tracker.Acknowledge(followerId, appliedIndex);

        int lwm = tracker.LowWaterMark();
        if (lwm > _truncatedBelow)
            Truncate(lwm);
    }

    private void Truncate(int upToExclusive)
    {
        int before = _log.Count;
        _log.RemoveAll(e => e.Index < upToExclusive);
        _truncatedBelow = upToExclusive;
        Console.WriteLine($"  [Log] Truncated below LWM={upToExclusive}: " +
                          $"removed {before - _log.Count} entries, {_log.Count} remain.");
    }

    // Follower requests entries it hasn't applied yet
    public IReadOnlyList<LogEntry> Since(int fromIndex) =>
        _log.Where(e => e.Index >= fromIndex).ToList();

    public int Count => _log.Count;
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Low-Water Mark ===\n");

var tracker = new FollowerTracker();
var log = new ReplicatedLog(tracker);

Console.WriteLine("--- Appending entries ---");
log.Append("user:1", "Alice");
log.Append("user:2", "Bob");
log.Append("user:3", "Carol");
log.Append("order:1", "order-data");
log.Append("order:2", "order-data-2");
Console.WriteLine($"\nLog size after appends: {log.Count}");

Console.WriteLine("\n--- Followers acknowledging entries ---");
log.OnFollowerAck("follower-A", 1);   // A is at index 1 — LWM=1
log.OnFollowerAck("follower-B", 3);   // B is at index 3 — LWM still 1 (min)

Console.WriteLine($"\n--- follower-A catches up ---");
log.OnFollowerAck("follower-A", 3);   // both at 3 — LWM=3, truncate below 3

Console.WriteLine($"\nFinal log size: {log.Count}  (only entries from index 3 onward remain)");

Console.WriteLine("\n--- New follower needs catch-up (from idx 4) ---");
var catchUp = log.Since(4);
Console.WriteLine($"  {catchUp.Count} entry(ies) to send to new follower:");
foreach (var e in catchUp)
    Console.WriteLine($"    idx={e.Index}  {e.Key}='{e.Value}'");
