# ❤️ Health Check

Services crash, dependencies become unavailable, and containers get restarted. Without a way to know whether a service is ready to handle requests, load balancers route traffic to broken instances and Kubernetes restarts healthy pods unnecessarily. The **Health Check** pattern exposes a dedicated endpoint that reports the service's liveness (is the process alive?) and readiness (is it ready to serve traffic?).

## Intent

> Expose `/health/live` and `/health/ready` endpoints so that orchestrators, load balancers, and monitoring systems can automatically route traffic away from unhealthy instances and restart truly failed ones.

## Problem

A service process may be running but unable to serve requests — its database connection pool is exhausted, a required upstream API is unreachable, or warm-up is incomplete. Without health checks, the orchestrator cannot distinguish "needs a restart" from "temporarily unavailable, route elsewhere."

### Bad Example

```csharp
// No health endpoint — load balancer uses TCP ping (just port open = healthy)
// A service with a broken DB connection still receives traffic
app.MapGet("/api/orders/{id}", async (string id, OrdersDbContext db) =>
{
    return await db.Orders.FindAsync(id);
});
// If DB is down, every request throws — but the load balancer sees no issue.
```

### Good Example

```csharp
// ── ASP.NET Core health checks (built-in) ─────────────
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self",     () => HealthCheckResult.Healthy())      // process alive
    .AddCheck("database", async ct =>                             // dependency healthy
    {
        bool canConnect = await CheckDatabaseAsync(ct);
        return canConnect
            ? HealthCheckResult.Healthy("DB reachable")
            : HealthCheckResult.Unhealthy("DB unreachable");
    })
    .AddCheck("rabbitmq", async ct =>
    {
        bool connected = await CheckRabbitMqAsync(ct);
        return connected
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded("MQ slow — processing may be delayed");
    });

app.MapHealthChecks("/health/live",  new HealthCheckOptions
{
    Predicate = check => check.Name == "self"   // only process liveness
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,                      // all checks — including dependencies
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Kubernetes probe configuration (in Deployment YAML):
// livenessProbe:  httpGet: { path: /health/live,  port: 8080 }
// readinessProbe: httpGet: { path: /health/ready, port: 8080 }

static Task<bool> CheckDatabaseAsync(CancellationToken ct)  => Task.FromResult(true);
static Task<bool> CheckRabbitMqAsync(CancellationToken ct)  => Task.FromResult(true);
```

## Key Takeaways

- **Liveness** (`/health/live`): answers "is the process alive?" — if it fails, restart the container.
- **Readiness** (`/health/ready`): answers "is the service ready to serve traffic?" — if it fails, remove from load balancer rotation but do NOT restart.
- Startup probes give a service time to warm up before liveness checks begin.
- `AspNetCore.HealthChecks.*` NuGet packages provide pre-built checks for SQL Server, Redis, RabbitMQ, and many others.
- Return `Degraded` (not `Unhealthy`) for non-critical dependencies to stay in rotation but signal reduced capacity.

## When to Use

- Every containerised or cloud-hosted microservice — this is a baseline requirement.
- Any service that depends on external resources (databases, message brokers, external APIs).
- When using Kubernetes, AWS ECS, or any orchestrator that drives traffic based on probe results.

## When NOT to Use

- There is no scenario where omitting health checks is acceptable for production microservices.
- Avoid making health checks too slow (> 1–2 s) — the orchestrator may time them out and false-positive restart.
