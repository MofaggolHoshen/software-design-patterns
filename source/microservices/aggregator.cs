// ============================================================
// Aggregator — C# Simulation
// ============================================================
//
// Intent: Compose a single response by calling multiple
// downstream services in parallel, shielding the client from
// the fanout complexity and the internal service topology.
//
// Key roles:
//   ProductService, ReviewService, InventoryService, PricingService
//   ProductAggregator — fans out and composes the response
// ============================================================

// ── Downstream service interfaces ─────────────────────────
record ProductInfo(string Id, string Name, string Description);
record ReviewStats(double Avg, int Total);
record StockStatus(int Available, string Warehouse);
record PriceInfo(decimal Amount, string Currency, decimal? DiscountedAmount);
record ProductPage(ProductInfo Product, ReviewStats Reviews,
                     StockStatus Stock, PriceInfo Pricing);

// ── Stub downstream services with simulated latency ───────
class ProductService
{
    public async Task<ProductInfo> GetAsync(string id)
    {
        await Task.Delay(15);    // simulated I/O
        Console.WriteLine($"  [Products]  Response for {id}");
        return new ProductInfo(id, "Ultralight Backpack", "Durable, 30L capacity");
    }
}

class ReviewService
{
    public async Task<ReviewStats> GetStatsAsync(string id)
    {
        await Task.Delay(25);
        Console.WriteLine($"  [Reviews]   Response for {id}");
        return new ReviewStats(4.6, 312);
    }
}

class InventoryService
{
    public async Task<StockStatus> GetAsync(string sku)
    {
        await Task.Delay(10);
        Console.WriteLine($"  [Inventory] Response for {sku}");
        return new StockStatus(Available: 18, Warehouse: "EU-West");
    }
}

class PricingService
{
    public async Task<PriceInfo> GetAsync(string id)
    {
        await Task.Delay(20);
        Console.WriteLine($"  [Pricing]   Response for {id}");
        return new PriceInfo(89.99m, "GBP", DiscountedAmount: 79.99m);
    }
}

// ── Aggregator ────────────────────────────────────────────
class ProductAggregator(
    ProductService products,
    ReviewService reviews,
    InventoryService inventory,
    PricingService pricing)
{
    public async Task<ProductPage> GetAsync(string productId)
    {
        Console.WriteLine($"[Aggregator] Fanning out to 4 services for '{productId}'...\n");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // All 4 calls in parallel — total latency ≈ max(individual latencies)
        var productTask = products.GetAsync(productId);
        var reviewsTask = reviews.GetStatsAsync(productId);
        var inventoryTask = inventory.GetAsync(productId);
        var pricingTask = pricing.GetAsync(productId);

        await Task.WhenAll(productTask, reviewsTask, inventoryTask, pricingTask);
        sw.Stop();

        Console.WriteLine($"\n[Aggregator] Composed in {sw.ElapsedMilliseconds} ms " +
                          $"(sequential would be ~{15 + 25 + 10 + 20} ms)\n");

        return new ProductPage(
            productTask.Result,
            reviewsTask.Result,
            inventoryTask.Result,
            pricingTask.Result);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Aggregator ===\n");

var aggregator = new ProductAggregator(
    new ProductService(), new ReviewService(),
    new InventoryService(), new PricingService());

var page = await aggregator.GetAsync("PROD-42");

Console.WriteLine("=== Composed Product Page ===");
Console.WriteLine($"  Name:       {page.Product.Name}");
Console.WriteLine($"  Desc:       {page.Product.Description}");
Console.WriteLine($"  Rating:     {page.Reviews.Avg}★ ({page.Reviews.Total} reviews)");
Console.WriteLine($"  Stock:      {page.Stock.Available} units @ {page.Stock.Warehouse}");
Console.WriteLine($"  Price:      {page.Pricing.Amount:C} {page.Pricing.Currency}" +
                  $"  (Sale: {page.Pricing.DiscountedAmount:C})");
Console.WriteLine("\nClient made ONE call — aggregator handled the fanout.");
