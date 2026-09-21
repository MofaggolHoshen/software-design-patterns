# 🔁 Request Pipeline

In a naïve request/response model the sender waits for each response before sending the next request, leaving the network idle for one round-trip latency between every send. **Request Pipeline** sends multiple requests in flight simultaneously without waiting for each individual response, saturating the link and multiplying effective throughput.

## Intent

> Send multiple requests to a server without waiting for each previous response, so that network and server capacity are fully utilised instead of being idle during round-trip wait times.

## Problem

Sequential send–wait–receive limits throughput to `1 / RTT` operations per second regardless of how fast the server can actually process them. At 10 ms RTT, you can do at most 100 ops/s sequentially — even if the server could handle 10,000 ops/s.

### Bad Example

```csharp
// Sequential: one RTT wasted per request
class SequentialClient(string endpoint)
{
    public async Task<string[]> FetchAllAsync(string[] ids)
    {
        var results = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            // await here means we send, wait one RTT, then send the next
            results[i] = await FetchOneAsync(ids[i]);
        }
        return results;
    }

    private async Task<string> FetchOneAsync(string id)
    {
        await Task.Delay(10); // simulate RTT
        return $"result:{id}";
    }
}
```

### Good Example

```csharp
// Pipelined: all requests in flight at once; collect responses independently
class PipelinedClient(string endpoint, int maxInFlight = 16)
{
    private readonly SemaphoreSlim _throttle = new(maxInFlight, maxInFlight);

    public async Task<string[]> FetchAllAsync(string[] ids)
    {
        // Fire up to maxInFlight requests simultaneously
        var tasks = ids.Select(id => FetchWithThrottleAsync(id));
        return await Task.WhenAll(tasks);
    }

    private async Task<string> FetchWithThrottleAsync(string id)
    {
        await _throttle.WaitAsync();
        try
        {
            return await FetchOneAsync(id);
        }
        finally
        {
            _throttle.Release();
        }
    }

    private async Task<string> FetchOneAsync(string id)
    {
        await Task.Delay(10); // simulate RTT
        return $"result:{id}";
    }
}
```

## Key Takeaways

- Pipeline depth (`maxInFlight`) controls the concurrency window. Too small → underutilised link; too large → server overload.
- TCP Nagle's algorithm can batch small pipelined frames automatically; HTTP/2 multiplexes streams natively.
- Redis, AMQP, and gRPC streaming all support pipelining as a first-class feature.
- Unlike batching, pipelining does not necessarily change the per-request protocol — it simply overlaps requests in time.
- Out-of-order responses are a concern: track request IDs and correlate responses to callers.

## When to Use

- Any client that makes many independent requests to the same server or service.
- Cache warming, bulk reads from a remote store, and parallel service calls in an aggregator.
- Any `async/await` code that currently `await`s in a loop — replace with `Task.WhenAll`.

## When NOT to Use

- When requests are causally dependent (result of request N feeds request N+1) — pipelining is not possible.
- When the server processes requests in strict FIFO order and a slow request starves later ones (head-of-line blocking).
- When circuit-breaker semantics are needed per request — large in-flight windows complicate error isolation.
