// ============================================================
// Strangler Fig — C# Simulation
// ============================================================
//
// Intent: Incrementally migrate a legacy monolith by routing
// traffic through a thin façade. Routes are switched one at
// a time from the monolith to new services — minimising risk
// and enabling rollback per route.
//
// Key roles:
//   ILegacyMonolith  — old system (forwards all requests)
//   IOrdersService   — new Orders microservice
//   StranglerFacade  — routing layer; switches per route
// ============================================================

// ── Legacy system ─────────────────────────────────────────
interface ILegacyMonolith
{
    Task<string> HandleAsync(string path, string method, string body);
}

class LegacyMonolith : ILegacyMonolith
{
    public Task<string> HandleAsync(string path, string method, string body)
    {
        Console.WriteLine($"  [Legacy] {method} {path}  body='{body}'");
        return Task.FromResult($"{{\"source\":\"monolith\",\"path\":\"{path}\"}}");
    }
}

// ── New microservice ───────────────────────────────────────
interface IOrdersService
{
    Task<string> GetOrderAsync(string id);
    Task<string> CreateOrderAsync(string body);
}

class OrdersMicroservice : IOrdersService
{
    public Task<string> GetOrderAsync(string id)
    {
        Console.WriteLine($"  [New Orders Service] GET /api/orders/{id}");
        return Task.FromResult($"{{\"source\":\"orders-service\",\"orderId\":\"{id}\"}}");
    }

    public Task<string> CreateOrderAsync(string body)
    {
        Console.WriteLine($"  [New Orders Service] POST /api/orders  body='{body}'");
        return Task.FromResult($"{{\"source\":\"orders-service\",\"created\":true}}");
    }
}

// ── Strangler Façade ──────────────────────────────────────
class StranglerFacade(ILegacyMonolith legacy, IOrdersService orders)
{
    // Routes registered here have been "strangled" to the new service.
    // All other routes still go to the legacy monolith.
    private readonly HashSet<string> _migratedPrefixes = new()
    {
        "/api/orders"
    };

    public async Task<string> RouteAsync(string method, string path, string body = "")
    {
        bool migrated = _migratedPrefixes.Any(p => path.StartsWith(p));

        if (migrated)
        {
            Console.WriteLine($"  [Facade] → new service: {method} {path}");
            // Route to the appropriate new service endpoint
            return path == "/api/orders" && method == "POST"
                ? await orders.CreateOrderAsync(body)
                : await orders.GetOrderAsync(path.Split('/').Last());
        }
        else
        {
            Console.WriteLine($"  [Facade] → legacy monolith: {method} {path}");
            return await legacy.HandleAsync(path, method, body);
        }
    }

    // Add a route to the migrated set (switch-over in production)
    public void Migrate(string prefix)
    {
        _migratedPrefixes.Add(prefix);
        Console.WriteLine($"  [Facade] Route '{prefix}' migrated to new service.");
    }

    // Roll back a migration (flip the switch back)
    public void Rollback(string prefix)
    {
        _migratedPrefixes.Remove(prefix);
        Console.WriteLine($"  [Facade] Route '{prefix}' rolled back to legacy.");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Strangler Fig ===\n");

var facade = new StranglerFacade(new LegacyMonolith(), new OrdersMicroservice());

Console.WriteLine("--- /api/orders already migrated ---");
Console.WriteLine(await facade.RouteAsync("GET", "/api/orders/ORDER-123"));
Console.WriteLine(await facade.RouteAsync("POST", "/api/orders", "{\"sku\":\"WIDGET\"}"));

Console.WriteLine("\n--- /api/customers still on legacy ---");
Console.WriteLine(await facade.RouteAsync("GET", "/api/customers/42"));

Console.WriteLine("\n--- Migrate /api/customers to new service ---");
facade.Migrate("/api/customers");
Console.WriteLine(await facade.RouteAsync("GET", "/api/customers/42"));

Console.WriteLine("\n--- Roll back /api/customers ---");
facade.Rollback("/api/customers");
Console.WriteLine(await facade.RouteAsync("GET", "/api/customers/42"));
