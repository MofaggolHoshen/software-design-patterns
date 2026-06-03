// ============================================================
// API Gateway — C# Simulation
// ============================================================
//
// Intent: Provide a single entry point for all client requests.
// The gateway handles authentication, rate-limiting, request
// aggregation, and routing so downstream services stay simple.
//
// Key roles:
//   ProductService, ReviewService, InventoryService — downstream
//   ApiGateway — single entry point (auth + aggregate + route)
// ============================================================

// ── Downstream microservices ───────────────────────────────
record ProductDto(string Id, string Name, decimal Price);
record ReviewSummary(double AvgRating, int Count);
record StockInfo(string Sku, int Available);
record ProductPage(ProductDto Product, ReviewSummary Reviews, StockInfo Stock);

class ProductService
{
    public Task<ProductDto> GetAsync(string id)
    {
        Console.WriteLine($"  [ProductService] GET product/{id}");
        return Task.FromResult(new ProductDto(id, "Premium Widget", 49.99m));
    }
}

class ReviewService
{
    public Task<ReviewSummary> GetStatsAsync(string productId)
    {
        Console.WriteLine($"  [ReviewService]  GET reviews/{productId}/stats");
        return Task.FromResult(new ReviewSummary(4.7, 138));
    }
}

class InventoryService
{
    public Task<StockInfo> GetStockAsync(string sku)
    {
        Console.WriteLine($"  [InventoryService] GET stock/{sku}");
        return Task.FromResult(new StockInfo(sku, 24));
    }
}

// ── Token store (simulated) ────────────────────────────────
static class TokenStore
{
    private static readonly Dictionary<string, string> _tokens = new()
    {
        ["tok-valid-abc"] = "user:42",
        ["tok-admin-xyz"] = "admin:1",
    };

    public static string? Resolve(string token) =>
        _tokens.TryGetValue(token, out var principal) ? principal : null;
}

// ── API Gateway ───────────────────────────────────────────
class ApiGateway(
    ProductService products,
    ReviewService reviews,
    InventoryService inventory,
    int rateLimit = 5)
{
    // Simplified per-principal rate limiting
    private readonly Dictionary<string, int> _requestCount = new();

    private string Authenticate(string token)
    {
        var principal = TokenStore.Resolve(token)
            ?? throw new UnauthorizedAccessException("Invalid or expired token.");
        Console.WriteLine($"  [Gateway] Authenticated as {principal}");
        return principal;
    }

    private void CheckRateLimit(string principal)
    {
        _requestCount.TryGetValue(principal, out int count);
        if (count >= rateLimit)
            throw new InvalidOperationException($"Rate limit exceeded for {principal}.");
        _requestCount[principal] = count + 1;
    }

    // Aggregation route: composes a product page from 3 services
    public async Task<ProductPage> GetProductPageAsync(string productId, string authToken)
    {
        var principal = Authenticate(authToken);
        CheckRateLimit(principal);

        // Fan out to downstream services in parallel
        var productTask = products.GetAsync(productId);
        var reviewTask = reviews.GetStatsAsync(productId);
        var stockTask = inventory.GetStockAsync(productId);

        await Task.WhenAll(productTask, reviewTask, stockTask);

        Console.WriteLine("  [Gateway] Aggregated 3 service responses.\n");
        return new ProductPage(productTask.Result, reviewTask.Result, stockTask.Result);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== API Gateway ===\n");

var gateway = new ApiGateway(
    new ProductService(), new ReviewService(), new InventoryService(),
    rateLimit: 3);

Console.WriteLine("--- Authenticated request ---");
var page = await gateway.GetProductPageAsync("PROD-007", authToken: "tok-valid-abc");
Console.WriteLine($"  Product:   {page.Product.Name} @ {page.Product.Price:C}");
Console.WriteLine($"  Reviews:   {page.Reviews.AvgRating}★ ({page.Reviews.Count} reviews)");
Console.WriteLine($"  Stock:     {page.Stock.Available} units\n");

Console.WriteLine("--- Unauthenticated request ---");
try { await gateway.GetProductPageAsync("PROD-007", authToken: "tok-invalid"); }
catch (UnauthorizedAccessException ex) { Console.WriteLine($"  Caught: {ex.Message}\n"); }

Console.WriteLine("--- Rate limit (make 3 calls then exceed) ---");
for (int i = 1; i <= 4; i++)
{
    try
    {
        await gateway.GetProductPageAsync("PROD-007", authToken: "tok-admin-xyz");
        Console.WriteLine($"  Call {i}: OK");
    }
    catch (InvalidOperationException ex) { Console.WriteLine($"  Call {i}: {ex.Message}"); }
}
