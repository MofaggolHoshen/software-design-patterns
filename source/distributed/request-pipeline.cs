// ============================================================
// Request Pipeline — C# Simulation
// ============================================================
//
// Intent: Send multiple requests concurrently without waiting
// for each individual response, so that network and server
// capacity are fully utilised instead of sitting idle during
// round-trip wait times.
//
// Demonstrates:
//   Sequential:  N requests × RTT  = total latency grows linearly
//   Pipelined:   max(RTT, N/capacity) = near-constant latency
// ============================================================

// ── Simulated remote service with artificial latency ───
class RemoteService
{
    private int _callCount;

    public async Task<string> FetchAsync(string id)
    {
        Interlocked.Increment(ref _callCount);
        await Task.Delay(20);   // simulate 20 ms RTT
        return $"data:{id}";
    }

    public int CallCount => _callCount;
}

// ── Sequential client (anti-pattern) ───────────────────
class SequentialClient(RemoteService svc)
{
    public async Task<string[]> FetchAllAsync(string[] ids)
    {
        var results = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++)
            results[i] = await svc.FetchAsync(ids[i]);  // wait each RTT
        return results;
    }
}

// ── Pipelined client (good pattern) ────────────────────
class PipelinedClient(RemoteService svc, int maxConcurrency = 16)
{
    private readonly SemaphoreSlim _throttle = new(maxConcurrency, maxConcurrency);

    public Task<string[]> FetchAllAsync(string[] ids) =>
        Task.WhenAll(ids.Select(FetchWithThrottleAsync));

    private async Task<string> FetchWithThrottleAsync(string id)
    {
        await _throttle.WaitAsync();
        try { return await svc.FetchAsync(id); }
        finally { _throttle.Release(); }
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Request Pipeline ===\n");

const int N = 40;
var ids = Enumerable.Range(1, N).Select(i => i.ToString()).ToArray();

// --- Sequential ---
var seqSvc = new RemoteService();
var seqClient = new SequentialClient(seqSvc);

Console.WriteLine($"Sequential: sending {N} requests one at a time...");
var sw1 = System.Diagnostics.Stopwatch.StartNew();
await seqClient.FetchAllAsync(ids);
sw1.Stop();
Console.WriteLine($"  Completed in {sw1.ElapsedMilliseconds} ms  ({seqSvc.CallCount} calls)\n");

// --- Pipelined ---
var pipSvc = new RemoteService();
var pipClient = new PipelinedClient(pipSvc, maxConcurrency: 16);

Console.WriteLine($"Pipelined:  sending {N} requests (max 16 in-flight)...");
var sw2 = System.Diagnostics.Stopwatch.StartNew();
await pipClient.FetchAllAsync(ids);
sw2.Stop();
Console.WriteLine($"  Completed in {sw2.ElapsedMilliseconds} ms  ({pipSvc.CallCount} calls)\n");

// --- Summary ---
Console.WriteLine("--- Summary ---");
Console.WriteLine($"Sequential:  {sw1.ElapsedMilliseconds,6} ms  (N × RTT)");
Console.WriteLine($"Pipelined:   {sw2.ElapsedMilliseconds,6} ms  (~RTT + scheduling overhead)");
Console.WriteLine($"Speedup:     {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F1}×");
