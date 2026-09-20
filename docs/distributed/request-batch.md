# 📦 Request Batch

Each individual network call incurs fixed per-request overhead: TCP round-trip, serialisation, framing, and server queueing. **Request Batch** collects multiple operations and sends them in a single network message, dramatically reducing overhead when throughput is more important than minimum latency.

## Intent

> Accumulate multiple requests over a short time window (or until a size limit is reached) and send them as a single network call, amortising per-request overhead across the batch.

## Problem

Sending one small command per network call is fine at low volume but degrades badly at scale. Each RPC adds TCP handshake overhead, involves separate serialisation/deserialisation cycles, and occupies a thread on the server. At 10,000 writes/second, individual calls saturate the network and overwhelm the server's thread pool.

### Bad Example

```csharp
// One cache write per network call — catastrophic at high throughput
class CacheClient(HttpClient http)
{
    public async Task SetAsync(string key, string value)
    {
        // One HTTP call per key — N keys = N round-trips
        await http.PostAsJsonAsync("/cache/set", new { key, value });
    }
}
```

### Good Example

```csharp
record SetCommand(string Key, string Value);

class BatchingCacheClient(HttpClient http, int maxBatch = 100, int flushMs = 10) : IAsyncDisposable
{
    private readonly Channel<SetCommand> _queue =
        Channel.CreateBounded<SetCommand>(new BoundedChannelOptions(10_000)
            { FullMode = BoundedChannelFullMode.Wait });

    private readonly CancellationTokenSource _cts = new();

    public BatchingCacheClient Start()
    {
        _ = FlushLoopAsync(_cts.Token);
        return this;
    }

    public ValueTask EnqueueAsync(string key, string value) =>
        _queue.Writer.WriteAsync(new SetCommand(key, value));

    private async Task FlushLoopAsync(CancellationToken ct)
    {
        var batch = new List<SetCommand>(maxBatch);
        while (!ct.IsCancellationRequested)
        {
            batch.Clear();
            var deadline = Task.Delay(flushMs, ct);

            // Drain up to maxBatch items or wait for the flush window
            while (batch.Count < maxBatch &&
                   _queue.Reader.TryRead(out var cmd))
                batch.Add(cmd);

            if (batch.Count == 0) { await deadline; continue; }

            await http.PostAsJsonAsync("/cache/batch-set", batch, ct);
            Console.WriteLine($"Flushed batch of {batch.Count} commands.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _cts.CancelAsync();
    }
}
```

## Key Takeaways

- Batching trades **latency** (individual commands wait up to `flushMs`) for **throughput** (far fewer round-trips).
- Tune both `maxBatch` (size) and `flushMs` (time) to balance latency vs. throughput for your workload.
- Kafka producers, Redis pipeline, and SQL `COPY` commands all use batching as their primary throughput mechanism.
- The server must expose a batch endpoint; a single-item batch and an N-item batch have the same fixed overhead.

## When to Use

- High-volume write paths: metrics, event ingestion, cache population, log shipping.
- Any client/server pair where per-call overhead is significant relative to payload size.
- Async pipelines where callers don't need an immediate per-item response.

## When NOT to Use

- Interactive UIs where latency matters more than throughput — a 10 ms flush delay is perceptible.
- Operations that must be immediately atomic (payment, reservation) — batch semantics add ambiguity around partial failures.
- When individual items must be acknowledged sequentially (ordered processing with at-most-once delivery).
