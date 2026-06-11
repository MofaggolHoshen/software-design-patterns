# 🌱 Strangler Fig

The **Strangler Fig** (or Strangler Pattern) is a migration strategy for incrementally replacing a legacy monolith. Traffic is routed through a façade that initially forwards everything to the monolith; over time, new microservices are built and registered behind the façade, and routes are switched one by one until the monolith can be retired — like a strangler fig vine gradually replacing the host tree.

## Intent

> Incrementally migrate a legacy system to microservices by routing traffic through a thin façade and switching individual routes from the old system to new services one at a time, minimising big-bang rewrite risk.

## Problem

Rewriting a monolith from scratch (the "big bang" rewrite) is extremely high risk: it takes years, the new system accumulates its own bugs, and the old system keeps evolving. The team ends up maintaining two systems simultaneously while the rewrite keeps getting pushed back.

### Bad Example

```csharp
// Big-bang rewrite — teams develop new system in parallel for 18 months
// The entire organisation halts feature development; the rewrite ships late
// and re-introduces half the bugs that were fixed in the monolith over years.

class BigBangRewriteApiController : ControllerBase
{
    // 50,000 lines of new code, zero traffic until the "go live" date
    // Single deployment event — failure means reverting everything
}
```

### Good Example

```csharp
// ── Strangler Façade — routes requests to old or new ──
class StranglerFacade(ILegacyMonolith legacy, IOrdersService newOrders)
{
    // Feature flag controls which routes are migrated
    private readonly HashSet<string> _migratedRoutes =
        new() { "/api/orders", "/api/orders/{id}" };

    public async Task<IActionResult> RouteAsync(HttpRequest request)
    {
        string path = request.Path.Value ?? "";

        bool isMigrated = _migratedRoutes.Any(r => PathMatches(path, r));

        if (isMigrated)
        {
            Console.WriteLine($"[Facade] → new service: {path}");
            return await RouteToNewServiceAsync(request, newOrders);
        }
        else
        {
            Console.WriteLine($"[Facade] → legacy monolith: {path}");
            return await RouteToMonolithAsync(request, legacy);
        }
    }

    private static bool PathMatches(string path, string template) =>
        path.StartsWith(template.Split('{')[0]);

    private Task<IActionResult> RouteToNewServiceAsync(HttpRequest req, IOrdersService svc) =>
        Task.FromResult<IActionResult>(new OkObjectResult("new-service-response"));

    private Task<IActionResult> RouteToMonolithAsync(HttpRequest req, ILegacyMonolith mono) =>
        Task.FromResult<IActionResult>(new OkObjectResult("legacy-response"));
}

interface ILegacyMonolith { }
interface IOrdersService  { }
```

## Key Takeaways

- The façade is the single point of routing and can be a reverse proxy (Nginx, YARP, AWS API Gateway).
- Migration happens route-by-route: build one new service, test it, switch one route, observe metrics, then proceed.
- Roll back is cheap — just flip the routing flag back to the monolith.
- The monolith and new services temporarily share a database or sync via events during transition; clean up the coupling after cutover.
- Named after the strangler fig plant that grows around a host tree, eventually replacing it.

## When to Use

- Migrating an existing monolith to microservices incrementally and safely.
- When you cannot freeze features on the old system during migration.
- When you need to validate each new service in production before retiring the legacy code.

## When NOT to Use

- Greenfield projects — build microservices directly; no monolith to strangle.
- When the monolith internals are so tangled that decomposing any single feature requires touching the entire codebase — invest in untangling the monolith first.
- When the façade adds latency that is unacceptable for all traffic (use shadow routing / canary instead).
