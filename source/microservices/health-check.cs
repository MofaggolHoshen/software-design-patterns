// ============================================================
// Health Check — C# Simulation
// ============================================================
//
// Intent: Expose /health/live and /health/ready endpoints so
// orchestrators and load balancers can detect unhealthy
// instances and route traffic or trigger restarts accordingly.
//
// Demonstrates:
//   - Liveness check (is the process running?)
//   - Readiness check (can it serve traffic? are deps healthy?)
//   - Degraded state (partial availability)
// ============================================================

enum HealthStatus { Healthy, Degraded, Unhealthy }

record HealthCheckResult(string Name, HealthStatus Status, string Description)
{
    public override string ToString() =>
        $"  {(Status == HealthStatus.Healthy ? "✓" : Status == HealthStatus.Degraded ? "⚠" : "✗")} " +
        $"{Name,-20} [{Status}]  {Description}";
}

record HealthReport(HealthStatus OverallStatus, IReadOnlyList<HealthCheckResult> Checks)
{
    public void Print()
    {
        Console.WriteLine($"  Overall: {OverallStatus}");
        foreach (var c in Checks) Console.WriteLine(c);
    }
}

// ── Individual health checks ───────────────────────────────
interface IHealthCheck
{
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}

class SelfCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default) =>
        Task.FromResult(new HealthCheckResult("self", HealthStatus.Healthy, "Process running."));
}

class DatabaseCheck(bool available) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        await Task.Delay(5, ct);  // simulate async probe
        var status = available ? HealthStatus.Healthy : HealthStatus.Unhealthy;
        var desc = available ? "Connection pool OK." : "Cannot reach SQL Server.";
        return new HealthCheckResult("database", status, desc);
    }
}

class MessageBrokerCheck(bool available) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        await Task.Delay(5, ct);
        var status = available ? HealthStatus.Healthy : HealthStatus.Degraded;
        var desc = available ? "RabbitMQ connected." : "RabbitMQ unavailable — async ops queued.";
        return new HealthCheckResult("rabbitmq", status, desc);
    }
}

class ExternalApiCheck(bool available) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        await Task.Delay(10, ct);
        var status = available ? HealthStatus.Healthy : HealthStatus.Degraded;
        return new HealthCheckResult("payment-api", status,
            available ? "Stripe API reachable." : "Stripe degraded — retries active.");
    }
}

// ── Health endpoint aggregator ─────────────────────────────
class HealthEndpoints(IReadOnlyList<IHealthCheck> checks)
{
    // Liveness: only the "self" check — tells Kubernetes to restart on failure
    public async Task<HealthReport> LiveAsync()
    {
        var self = await new SelfCheck().CheckAsync();
        return new HealthReport(self.Status, [self]);
    }

    // Readiness: all checks — tells load balancer to remove from rotation
    public async Task<HealthReport> ReadyAsync()
    {
        var results = await Task.WhenAll(checks.Select(c => c.CheckAsync()));
        var overall = results.Any(r => r.Status == HealthStatus.Unhealthy) ? HealthStatus.Unhealthy
                    : results.Any(r => r.Status == HealthStatus.Degraded) ? HealthStatus.Degraded
                    : HealthStatus.Healthy;
        return new HealthReport(overall, results);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Health Check ===\n");

Console.WriteLine("--- Scenario 1: All dependencies healthy ---");
var healthy = new HealthEndpoints([
    new SelfCheck(),
    new DatabaseCheck(available: true),
    new MessageBrokerCheck(available: true),
    new ExternalApiCheck(available: true)
]);
Console.WriteLine("GET /health/live:");
(await healthy.LiveAsync()).Print();
Console.WriteLine();
Console.WriteLine("GET /health/ready:");
(await healthy.ReadyAsync()).Print();

Console.WriteLine("\n--- Scenario 2: Database down (service unready) ---");
var dbDown = new HealthEndpoints([
    new SelfCheck(),
    new DatabaseCheck(available: false),
    new MessageBrokerCheck(available: true),
    new ExternalApiCheck(available: true)
]);
Console.WriteLine("GET /health/ready:");
(await dbDown.ReadyAsync()).Print();
Console.WriteLine("→ Load balancer removes this instance from rotation.");

Console.WriteLine("\n--- Scenario 3: Only broker degraded (still ready) ---");
var brokerDegraded = new HealthEndpoints([
    new SelfCheck(),
    new DatabaseCheck(available: true),
    new MessageBrokerCheck(available: false),
    new ExternalApiCheck(available: false)
]);
Console.WriteLine("GET /health/ready:");
(await brokerDegraded.ReadyAsync()).Print();
Console.WriteLine("→ Service stays in rotation but monitoring alerts on Degraded.");
