# 🚪 API Gateway

In a microservices architecture, clients would otherwise need to know the address of every downstream service, handle authentication separately per service, and aggregate responses themselves. The **API Gateway** pattern inserts a single-entry-point proxy that handles cross-cutting concerns — routing, authentication, rate limiting, request aggregation, and protocol translation — so downstream services remain simple.

## Intent

> Provide a single entry point for all client requests that handles cross-cutting concerns (auth, rate-limiting, logging, SSL termination) and routes or aggregates requests to downstream microservices.

## Problem

Without a gateway, a mobile app must call the Orders service, then the Inventory service, then the Ratings service — authenticating separately with each, handling partial failures, and aggregating three responses. Each downstream service must re-implement auth and rate-limiting, causing duplication and inconsistency.

### Bad Example

```csharp
// Client must call every service individually, handle auth per-service,
// and merge results — tight coupling to internal service topology
class MobileApp
{
    public async Task<ProductPage> LoadProductPageAsync(string productId)
    {
        // Must know 3 different hostnames; impossible to refactor backend
        var product  = await _productHttpClient.GetProductAsync(productId);
        var reviews  = await _reviewHttpClient.GetReviewsAsync(productId);
        var stock    = await _inventoryHttpClient.GetStockAsync(productId);

        // Client owns aggregation logic; any backend restructure breaks app
        return new ProductPage(product, reviews, stock);
    }
}
```

### Good Example

```csharp
// ── Downstream services ───────────────────────────────
record ProductDto(string Id, string Name, decimal Price);
record ReviewDto (string ProductId, int Rating, string Text);
record StockDto  (string Sku, int Available);

class ProductService  { public Task<ProductDto>  GetAsync(string id) =>
    Task.FromResult(new ProductDto(id, "Widget", 9.99m)); }
class ReviewService   { public Task<ReviewDto[]> GetAsync(string id) =>
    Task.FromResult<ReviewDto[]>([new(id, 5, "Excellent!")]); }
class InventoryService{ public Task<StockDto>    GetAsync(string sku) =>
    Task.FromResult(new StockDto(sku, 42)); }

// ── API Gateway ───────────────────────────────────────
class ApiGateway(
    ProductService   products,
    ReviewService    reviews,
    InventoryService inventory)
{
    // Cross-cutting: authentication
    private bool Authenticate(string token)
    {
        Console.WriteLine($"  [Gateway] Authenticating token '{token[..8]}...'");
        return !string.IsNullOrEmpty(token);
    }

    // Aggregation: one client call fetches data from 3 services
    public async Task<object> GetProductPageAsync(string id, string authToken)
    {
        if (!Authenticate(authToken))
            throw new UnauthorizedAccessException("Invalid token.");

        // Fan out to downstream services in parallel
        var (product, revs, stock) = await (
            products.GetAsync(id),
            reviews.GetAsync(id),
            inventory.GetAsync(id)
        ).WhenAll();

        Console.WriteLine("  [Gateway] Aggregated 3 service responses.");
        return new { product, reviews = revs, stock };
    }
}

static class TaskExtensions
{
    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(
        this (Task<T1> t1, Task<T2> t2, Task<T3> t3) tasks)
    {
        await Task.WhenAll(tasks.t1, tasks.t2, tasks.t3);
        return (tasks.t1.Result, tasks.t2.Result, tasks.t3.Result);
    }
}
```

## Key Takeaways

- The gateway is the single point for **authentication**, **authorisation**, **rate limiting**, **SSL termination**, and **observability**.
- Downstream services trust that the gateway has already authenticated the request (pass JWT claims in a header).
- **Backend for Frontend (BFF)** is a specialised gateway tailored to a specific client type (mobile, web, 3rd-party).
- Popular implementations: AWS API Gateway, Kong, NGINX, YARP (.NET), Ocelot (.NET).

## When to Use

- Any microservices architecture with more than one downstream service.
- When mobile/web clients need a single hostname to target regardless of backend topology.
- When you need to centralise cross-cutting concerns away from downstream services.

## When NOT to Use

- A single-service system — the gateway adds a hop without benefit.
- When the gateway becomes a "smart pipe" that encodes business logic — keep it a thin proxy.
- If the gateway becomes a single team's bottleneck for all routing changes — prefer a **sidecar** or **service mesh** (Istio, Linkerd) for service-to-service traffic.
