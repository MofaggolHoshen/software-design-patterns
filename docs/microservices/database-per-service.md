# 🗄️ Database per Service

Sharing a database between microservices couples their schemas, deployment cycles, and scaling characteristics. The **Database per Service** pattern gives each microservice exclusive ownership of its own data store, enabling independent schema evolution, polyglot persistence, and true loose coupling.

## Intent

> Each microservice owns and manages its private data store; no other service can access that data directly — only through the owning service's public API or published events.

## Problem

When two services share a database table, a schema change in one service requires coordination with the other. One service's heavy queries starve the other. A schema migration cannot be deployed independently, negating the value of independent deployability.

### Bad Example

```csharp
// Both OrdersService and InventoryService query the same shared database
// Tight schema coupling; schema migration requires both teams to coordinate
class OrdersService(SharedDbContext db)
{
    public async Task<Order> GetOrderAsync(int id) =>
        await db.Orders
                .Include(o => o.Items)            // shared table
                .Include(o => o.InventoryItems)   // InventoryService's table!
                .FirstAsync(o => o.Id == id);
}

class InventoryService(SharedDbContext db)
{
    // Also accesses Orders table — tight coupling
    public async Task<int> GetAvailableStockAsync(string sku) =>
        await db.InventoryItems.Where(i => i.Sku == sku).SumAsync(i => i.Qty);
}
```

### Good Example

```csharp
// ── Each service has its own DbContext / connection string ──
class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    // Only Orders data — never references InventoryItem directly
}

class InventoryDbContext : DbContext
{
    public DbSet<InventoryItem> Items => Set<InventoryItem>();
    // Only Inventory data — completely independent schema
}

// ── Cross-service data access is via API or events ─────────
class OrdersService(OrdersDbContext db, IInventoryServiceClient inventoryClient)
{
    public async Task<bool> PlaceOrderAsync(string sku, int qty)
    {
        // Query inventory via HTTP API — NOT direct DB join
        bool inStock = await inventoryClient.IsAvailableAsync(sku, qty);
        if (!inStock) return false;

        db.Orders.Add(new Order { Sku = sku, Quantity = qty });
        await db.SaveChangesAsync();
        Console.WriteLine($"  [Orders] Order saved in Orders DB.");
        return true;
    }
}

class InventoryService(InventoryDbContext db)
{
    public async Task<bool> IsAvailableAsync(string sku, int qty)
    {
        var item = await db.Items.FindAsync(sku);
        return item is not null && item.Qty >= qty;
    }
}

// ── Placeholder types ──────────────────────────────────────
record Order    { public string Sku { get; set; } = ""; public int Quantity { get; set; } }
record InventoryItem { public string Sku { get; set; } = ""; public int Qty { get; set; } }
interface IInventoryServiceClient { Task<bool> IsAvailableAsync(string sku, int qty); }
```

## Key Takeaways

- **Polyglot persistence**: Orders might use PostgreSQL, Inventory a document store, Search an Elasticsearch index — each service picks the best fit.
- Eventual consistency replaces ACID joins; use **Saga** or **CQRS projections** to synchronise data across service boundaries.
- The data contract between services is the **public API** or **domain event**, not the schema.
- Independent deployability becomes real: change the schema of Orders without touching Inventory.

## When to Use

- Any microservices architecture where services must be independently deployable and scalable.
- When different services have wildly different storage requirements (relational vs. document vs. time-series).
- When schema coupling is already blocking teams from deploying independently.

## When NOT to Use

- When you need strong ACID transactions spanning multiple entities — rethink the service boundaries first.
- Small teams or single applications where the overhead of managing multiple databases exceeds the benefit.
- Reporting and analytics: querying across service databases is unavoidable; use a read model, CQRS, or a data warehouse instead.
