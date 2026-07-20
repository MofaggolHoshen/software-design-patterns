// ============================================================
// Request Batch — C# Simulation
// ============================================================
//
// Intent: Accumulate multiple requests over a short time window
// or until a size limit is reached, then send them as a single
// network call — amortising per-request overhead across the
// entire batch to dramatically increase throughput.
//
// Key roles:
//   FakeServer       — simulates a batch-aware endpoint
//   BatchingClient   — buffers writes and flushes as batches
// ============================================================

using System.Threading.Channels;

// ── Simulated server that accepts batch writes ─────────
class FakeServer
{
    private readonly Dictionary<string, string> _store = new();
    private int _batchCount;

    public Task<int> BatchSetAsync(IReadOnlyList<(string Key, string Value)> items)
    {
        foreach (var (k, v) in items)
            _store[k] = v;

        _batchCount++;
        Console.WriteLine($"  [Server] Batch #{_batchCount}: received {items.Count} items in one call.");
        return Task.FromResult(_batchCount);
    }

    public int BatchesReceived => _batchCount;
    public int ItemsStored => _store.Count;
}

// ── Batching client ────────────────────────────────────
class BatchingClient(FakeServer server, int maxBatchSize = 50, int flushIntervalMs = 20)
    : IAsyncDisposable
{
    private readonly Channel<(string Key, string Value)> _queue =
        Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(10_000)
        { FullMode = BoundedChannelFullMode.Wait });

    private readonly CancellationTokenSource _cts = new();
    private Task? _flushTask;

    public BatchingClient Start()
    {
        _flushTask = FlushLoopAsync(_cts.Token);
        return this;
    }

    // Callers just enqueue — they don't wait for the network round-trip
    public ValueTask EnqueueAsync(string key, string value) =>
        _queue.Writer.WriteAsync((key, value));

    private async Task FlushLoopAsync(CancellationToken ct)
    {
        var batch = new List<(string Key, string Value)>(maxBatchSize);

        while (!ct.IsCancellationRequested)
        {
            batch.Clear();

            // Drain up to maxBatchSize items right now
            while (batch.Count < maxBatchSize && _queue.Reader.TryRead(out var cmd))
                batch.Add(cmd);

            if (batch.Count > 0)
            {
                await server.BatchSetAsync(batch);
            }

            // Wait for next flush window
            await Task.Delay(flushIntervalMs, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.Complete();
        await _cts.CancelAsync();
        if (_flushTask is not null) await _flushTask.ConfigureAwait(false);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Request Batch ===\n");

var server = new FakeServer();
await using var client = new BatchingClient(server, maxBatchSize: 25, flushIntervalMs: 30).Start();

const int TotalItems = 200;
Console.WriteLine($"Enqueuing {TotalItems} individual items...\n");

for (int i = 0; i < TotalItems; i++)
    await client.EnqueueAsync($"key:{i}", $"value:{i}");

// Give the flush loop time to drain
await Task.Delay(200);

Console.WriteLine($"\n--- Results ---");
Console.WriteLine($"Items sent:     {TotalItems}");
Console.WriteLine($"Network calls:  {server.BatchesReceived}  (batched, ~{TotalItems / Math.Max(1, server.BatchesReceived):F0} items/call)");
Console.WriteLine($"Items stored:   {server.ItemsStored}");
Console.WriteLine($"\nWithout batching: {TotalItems} network calls.");
Console.WriteLine($"With batching:    {server.BatchesReceived} network calls.");
Console.WriteLine($"Reduction:        {(1.0 - (double)server.BatchesReceived / TotalItems) * 100:F0}%");
