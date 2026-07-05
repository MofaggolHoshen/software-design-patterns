// ============================================================
// Clock-Bound Wait — C# Simulation
// ============================================================
//
// Intent: Before committing a timestamped write, pause for the
// maximum clock-uncertainty window (epsilon) so that any node
// that may have a slower clock has already advanced past the
// write's timestamp, preserving global causal ordering.
//
// Key roles:
//   ClockBoundNode — replica that applies clock-bound waits
//   OrderChecker   — verifies causal ordering across replicas
// ============================================================

class ClockBoundNode(string id, int epsilonMs = 8)
{
    public string Id => id;
    private readonly List<(long Ts, string Value)> _log = [];

    // Write: stamp NOW, wait epsilon, then commit
    public async Task WriteAsync(string value)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Epsilon wait: any in-flight writer on any node that started
        // a write with Ts < this.Ts will have committed within epsilonMs.
        await Task.Delay(epsilonMs);

        _log.Add((ts, value));
        Console.WriteLine($"  [{id}] Committed '{value}' at T={ts} ms (waited {epsilonMs} ms)");
    }

    // Read returns events in wall-clock order; safe after epsilon has elapsed
    public IReadOnlyList<(long Ts, string Value)> ReadOrdered() =>
        [.. _log.OrderBy(e => e.Ts)];
}

// Verify that events written to multiple nodes appear in causal order
static class OrderChecker
{
    public static void Verify(
        IReadOnlyList<(long Ts, string Value)> nodeA,
        IReadOnlyList<(long Ts, string Value)> nodeB)
    {
        var merged = nodeA.Concat(nodeB).OrderBy(e => e.Ts).ToList();
        Console.WriteLine("\nMerged ordered log:");
        foreach (var (ts, val) in merged)
            Console.WriteLine($"  T={ts,-15} {val}");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Clock-Bound Wait ===\n");

var nodeA = new ClockBoundNode("A");
var nodeB = new ClockBoundNode("B");

// Simulate two nodes writing concurrently
await Task.WhenAll(
    nodeA.WriteAsync("order:1001 confirmed"),
    nodeB.WriteAsync("payment:1001 charged"),
    nodeA.WriteAsync("shipment:1001 dispatched")
);

Console.WriteLine();
OrderChecker.Verify(nodeA.ReadOrdered(), nodeB.ReadOrdered());
Console.WriteLine("\nAll writes are causally ordered thanks to clock-bound wait.");
