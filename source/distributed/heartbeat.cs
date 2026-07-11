// ============================================================
// HeartBeat — C# Simulation
// ============================================================
//
// Intent: Have each node send periodic "I am alive" signals.
// The absence of a heartbeat within a timeout window is
// treated as node failure, triggering automatic recovery
// actions such as leader re-election or service deregistration.
//
// Key roles:
//   HeartbeatSender   — background loop that emits heartbeats
//   FailureDetector   — records last-seen times, detects failures
//   ClusterMonitor    — reacts to detected failures
// ============================================================

class HeartbeatSender(string nodeId, TimeSpan interval)
{
    public string NodeId => nodeId;

    public async Task RunAsync(FailureDetector detector, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            detector.RecordHeartbeat(nodeId);
            await Task.Delay(interval, ct).ConfigureAwait(false);
        }
        Console.WriteLine($"  [{nodeId}] Heartbeat loop stopped (node down).");
    }
}

class FailureDetector(TimeSpan failureTimeout)
{
    private readonly Dictionary<string, DateTimeOffset> _lastSeen = new();
    private readonly object _lock = new();

    public void RecordHeartbeat(string nodeId)
    {
        lock (_lock)
        {
            _lastSeen[nodeId] = DateTimeOffset.UtcNow;
        }
        Console.WriteLine($"  [HB] ♥ {nodeId} at {DateTimeOffset.UtcNow:HH:mm:ss.fff}");
    }

    // Returns IDs of nodes that haven't sent a heartbeat within the timeout
    public IReadOnlyList<string> GetFailedNodes()
    {
        var threshold = DateTimeOffset.UtcNow - failureTimeout;
        lock (_lock)
        {
            return _lastSeen
                .Where(kv => kv.Value < threshold)
                .Select(kv => kv.Key)
                .ToList();
        }
    }

    public IReadOnlyList<string> RegisteredNodes
    {
        get { lock (_lock) { return [.. _lastSeen.Keys]; } }
    }
}

class ClusterMonitor(FailureDetector detector, TimeSpan pollInterval)
{
    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(pollInterval, ct).ConfigureAwait(false);
            var failed = detector.GetFailedNodes();
            if (failed.Count > 0)
            {
                foreach (var node in failed)
                    Console.WriteLine($"  [Monitor] ⚠  FAILURE detected: {node} — triggering recovery.");
            }
        }
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== HeartBeat ===\n");

var detector = new FailureDetector(failureTimeout: TimeSpan.FromMilliseconds(400));
var monitor = new ClusterMonitor(detector, pollInterval: TimeSpan.FromMilliseconds(200));

using var globalCts = new CancellationTokenSource();
using var node1Cts = new CancellationTokenSource();
using var node2Cts = new CancellationTokenSource();

var sender1 = new HeartbeatSender("node-1", TimeSpan.FromMilliseconds(150));
var sender2 = new HeartbeatSender("node-2", TimeSpan.FromMilliseconds(150));

// Start heartbeats and monitor
_ = sender1.RunAsync(detector, node1Cts.Token);
_ = sender2.RunAsync(detector, node2Cts.Token);
_ = monitor.MonitorAsync(globalCts.Token);

Console.WriteLine("--- All nodes healthy (500 ms) ---");
await Task.Delay(500);

// Simulate node-1 crashing
Console.WriteLine("\n--- node-1 CRASHES ---");
await node1Cts.CancelAsync();
await Task.Delay(700);   // wait > failureTimeout so monitor can fire

Console.WriteLine("\n--- node-2 still running, stopping demo ---");
await globalCts.CancelAsync();
await node2Cts.CancelAsync();
await Task.Delay(50);    // let cancellations propagate

Console.WriteLine("\nRegistered nodes at shutdown: " +
                  string.Join(", ", detector.RegisteredNodes));
