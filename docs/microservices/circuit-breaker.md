# ⚡ Circuit Breaker

When a downstream service is slow or returning errors, every incoming request waits for the timeout before failing. Under high load, all threads pile up waiting, exhausting resources and causing a **cascading failure** across the entire system. The **Circuit Breaker** pattern wraps calls to a downstream dependency and, after a threshold of failures, opens a "circuit" to fail fast — giving the downstream service time to recover and preventing resource exhaustion.

## Intent

> Monitor calls to a downstream dependency; after a threshold of failures, open the circuit to reject calls immediately (fail fast) for a cooldown period, then allow a probe call through to check if the service has recovered.

## Problem

Service A calls Service B. B is slow (5 s timeout). 100 requests/s arrive. Within seconds, 500 threads are blocked waiting for B to respond. Thread pool exhaustion means Service A starts failing all requests, including those that don't depend on B — a cascading failure.

### Bad Example

```csharp
// No circuit breaker — every call waits for timeout; threads pile up
class PaymentClient(HttpClient http)
{
    public async Task<string> ChargeAsync(string customerId, decimal amount)
    {
        // If the payment service is down, every call blocks for 30s
        var response = await http.PostAsJsonAsync("/charge", new { customerId, amount });
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Good Example

```csharp
enum CircuitState { Closed, Open, HalfOpen }

class CircuitBreaker<T>(
    Func<Task<T>> action,
    int           failureThreshold  = 3,
    TimeSpan?     cooldown          = null)
{
    private readonly TimeSpan _cooldown  = cooldown ?? TimeSpan.FromSeconds(30);
    private CircuitState  _state         = CircuitState.Closed;
    private int           _failureCount;
    private DateTimeOffset _openedAt;

    public async Task<T> ExecuteAsync()
    {
        switch (_state)
        {
            case CircuitState.Open:
                if (DateTimeOffset.UtcNow - _openedAt < _cooldown)
                {
                    Console.WriteLine($"  [CB] Circuit OPEN — failing fast.");
                    throw new CircuitBreakerOpenException("Circuit is open.");
                }
                _state = CircuitState.HalfOpen;
                Console.WriteLine($"  [CB] Circuit HALF-OPEN — probe call...");
                break;

            case CircuitState.HalfOpen:
                // Only one probe call is allowed through
                break;
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
        Console.WriteLine("  [CB] Call succeeded — circuit CLOSED.");
    }

    private void OnFailure()
    {
        _failureCount++;
        Console.WriteLine($"  [CB] Failure #{_failureCount}.");
        if (_failureCount >= failureThreshold)
        {
            _state    = CircuitState.Open;
            _openedAt = DateTimeOffset.UtcNow;
            Console.WriteLine($"  [CB] Threshold reached — circuit OPENED (cooldown {_cooldown}).");
        }
    }
}

class CircuitBreakerOpenException(string msg) : Exception(msg);
```

## Key Takeaways

- **Closed**: normal operation; failures are counted.
- **Open**: all calls fail fast for the cooldown period — no load on the downstream service.
- **Half-Open**: one probe call allowed; success → Closed; failure → Open again.
- Libraries: **Polly** (most popular .NET resilience library) implements circuit breakers, retries, and bulkheads.
- Combine with a **fallback**: return cached data or a degraded response instead of an error when the circuit is open.

## When to Use

- Any inter-service HTTP or gRPC call in a microservices architecture.
- Calls to external third-party APIs that can experience outages.
- Any blocking I/O that, if it hangs, could exhaust a thread pool or connection pool.

## When NOT to Use

- Idempotent reads with very short timeouts where failing fast is already the default (< 50 ms).
- Local, in-process calls that cannot cascade failures.
- When the operation must succeed or the entire business process should stop — don't silently swallow errors behind a fallback.
