# 🧩 Aggregator

When a client needs data from multiple microservices, having it fan out and merge those responses creates tight coupling between the client and the internal topology. The **Aggregator** pattern places the composition logic in a dedicated service or gateway route that calls downstream services in parallel, merges the results, and returns a single cohesive response.

## Intent

> Provide a service that calls multiple downstream microservices, aggregates their responses, and returns a single composed payload — shielding the client from the fanout complexity and the internal service topology.

## Problem

A product detail page requires data from Products, Reviews, Inventory, and Pricing services. Without an aggregator, either the client makes four round-trips (slow, tightly coupled) or each service must know about the others (circular dependencies).

### Bad Example

```csharp
// Client makes 4 sequential calls — slow and tightly coupled to topology
class ProductPageClient
{
    HttpClient _http;

    public async Task<ProductPage> GetAsync(string id)
    {
        // Sequential: 4 × RTT latency
        var product  = await _http.GetFromJsonAsync<ProductDto>  ($"/products/{id}");
        var reviews  = await _http.GetFromJsonAsync<ReviewDto[]> ($"/reviews/{id}");
        var stock    = await _http.GetFromJsonAsync<StockDto>    ($"/inventory/{id}");
        var price    = await _http.GetFromJsonAsync<PriceDto>    ($"/pricing/{id}");
        return new ProductPage(product!, reviews!, stock!, price!);
    }
}
```

### Good Example

```csharp
// ── Downstream service interfaces ─────────────────────
record ProductDto (string Id, string Name);
record ReviewDto  (double AvgRating, int Count);
record StockDto   (int Available);
record PriceDto   (decimal Amount, string Currency);
record ProductPage(ProductDto Product, ReviewDto Reviews,
                   StockDto Stock, PriceDto Price);

interface IProductService  { Task<ProductDto>  GetAsync(string id); }
interface IReviewService   { Task<ReviewDto>   GetStatsAsync(string id); }
interface IInventoryService{ Task<StockDto>    GetAsync(string sku); }
interface IPricingService  { Task<PriceDto>    GetAsync(string id); }

// ── Aggregator ────────────────────────────────────────
class ProductAggregator(
    IProductService   products,
    IReviewService    reviews,
    IInventoryService inventory,
    IPricingService   pricing)
{
    public async Task<ProductPage> GetProductPageAsync(string productId)
    {
        // Fan out to all four services in parallel — only max(RTT) latency
        var productTask  = products.GetAsync(productId);
        var reviewsTask  = reviews.GetStatsAsync(productId);
        var stockTask    = inventory.GetAsync(productId);
        var priceTask    = pricing.GetAsync(productId);

        await Task.WhenAll(productTask, reviewsTask, stockTask, priceTask);

        Console.WriteLine("  [Aggregator] All 4 services responded — composing response.");
        return new ProductPage(
            productTask.Result,
            reviewsTask.Result,
            stockTask.Result,
            priceTask.Result);
    }
}
```

## Key Takeaways

- Fan-out to downstream services **in parallel** (`Task.WhenAll`) so total latency equals the slowest single service, not the sum.
- The aggregator owns the composition contract — clients are shielded from backend topology changes.
- Use **partial responses** when downstream services are optional: return data even if one service is slow or fails.
- The aggregator is often co-located with the API Gateway (BFF) or implemented as a dedicated service.
- Combine with **Circuit Breaker** to degrade gracefully when downstream services are unavailable.

## When to Use

- A client needs a joined view of data owned by multiple services.
- When you want to hide the service topology from external clients.
- Building a Backend-for-Frontend (BFF) that tailors the response shape to a specific client.

## When NOT to Use

- When a single service already owns all the required data — no aggregation needed.
- When downstream call counts are very large (N+1 problem) — prefer a read model or event-sourced projection instead.
- If the aggregator starts encoding business logic — keep it a thin composition layer; move business rules to the domain services.
