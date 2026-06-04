// ============================================================
// Circuit Breaker — C# Simulation
// ============================================================
//
// Intent: After a threshold of failures, open the circuit to
// fail fast — preventing cascading failures and giving the
// downstream service time to recover.
//
// States:
//   Closed   → normal operation; failures counted
//   Open     → all calls fail fast for cooldown period
//   HalfOpen → one probe call; success→Closed, fail→Open
// ============================================================

enum CircuitState { Closed, Open, HalfOpen }

class CircuitBreakerOpenException(string msg) : Exception(msg);

class CircuitBreaker(
    string name,
    int failureThreshold = 3,
    TimeSpan? cooldown = null)
{
    private readonly TimeSpan _cooldown = cooldown ?? TimeSpan.FromSeconds(5);
    private CircuitState _state = CircuitState.Closed;
    private int _failures;
    private DateTimeOffset _openedAt;
    private int _probeAllowed;   // 1 = probe in progress

    public CircuitState State => _state;
    public string Name => name;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        switch (_state)
        {
            case CircuitState.Open:
                if (DateTimeOffset.UtcNow - _openedAt < _cooldown)
                {
                    Console.WriteLine($"  [{name}] OPEN → fail fast");
                    throw new CircuitBreakerOpenException($"Circuit '{name}' is open.");
                }
                // Transition to half-open: allow one probe
                if (Interlocked.Exchange(ref _probeAllowed, 1) == 0)
                {
                    _state = CircuitState.HalfOpen;
                    Console.WriteLine($"  [{name}] → HALF-OPEN (probe)");
                }
                else
                {
                    throw new CircuitBreakerOpenException($"Circuit '{name}' is half-open, probe in progress.");
                }
                break;
        }

        try
        {
            var result = await action();
            RecordSuccess();
            return result;
        }
        catch (CircuitBreakerOpenException) { throw; }
        catch (Exception ex)
        {
            RecordFailure(ex);
            throw;
        }
    }

    private void RecordSuccess()
    {
        _failures = 0;
        _probeAllowed = 0;
        _state = CircuitState.Closed;
        Console.WriteLine($"  [{name}] SUCCESS → CLOSED");
    }

    private void RecordFailure(Exception ex)
    {
        _failures++;
        Console.WriteLine($"  [{name}] FAILURE #{_failures}: {ex.Message}");
        if (_failures >= failureThreshold || _state == CircuitState.HalfOpen)
        {
            _state = CircuitState.Open;
            _openedAt = DateTimeOffset.UtcNow;
            _probeAllowed = 0;
            Console.WriteLine($"  [{name}] → OPEN (cooldown {_cooldown})");
        }
    }
}

// ── Simulated downstream service ──────────────────────────
class PaymentGateway(int failForFirstNCalls = 0)
{
    private int _callCount;

    public async Task<string> ChargeAsync(string customerId, decimal amount)
    {
        await Task.Delay(10);
        int call = Interlocked.Increment(ref _callCount);
        if (call <= failForFirstNCalls)
            throw new HttpRequestException($"Gateway timeout (call #{call})");

        return $"CHG-{Guid.NewGuid():N}"[..12].ToUpper();
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Circuit Breaker ===\n");

// Gateway will fail the first 4 calls, then recover
var gateway = new PaymentGateway(failForFirstNCalls: 4);
var cb = new CircuitBreaker("payment-gateway",
                                 failureThreshold: 3,
                                 cooldown: TimeSpan.FromSeconds(2));

async Task TryCharge(string label)
{
    try
    {
        var chargeId = await cb.ExecuteAsync(() => gateway.ChargeAsync("cust-1", 29.99m));
        Console.WriteLine($"  [{label}] Charged → {chargeId}  state={cb.State}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{label}] Error: {ex.Message}  state={cb.State}");
    }
}

Console.WriteLine("--- 3 failures → circuit opens ---");
await TryCharge("call-1");
await TryCharge("call-2");
await TryCharge("call-3");

Console.WriteLine("\n--- Subsequent calls fail fast ---");
await TryCharge("call-4");
await TryCharge("call-5");

Console.WriteLine("\n--- Wait for cooldown, then probe ---");
await Task.Delay(2500);
await TryCharge("probe");   // probe succeeds (gateway recovered after 4 failures)

Console.WriteLine("\n--- Circuit closed; normal operation resumes ---");
await TryCharge("call-6");
await TryCharge("call-7");
