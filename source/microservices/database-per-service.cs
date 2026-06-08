// ============================================================
// Database per Service — C# Simulation
// ============================================================
//
// Intent: Each microservice exclusively owns its private data
// store; other services can only access the data through the
// owning service's public API — never by direct DB access.
//
// Demonstrates:
//   - Each service has its own in-memory "database"
//   - Cross-service reads go through HTTP API (simulated)
//   - Schema changes are isolated to the owning service
// ============================================================

// ── Orders Service — owns its own order store ─────────────
record Order(string OrderId, string CustomerId, string Sku,
             int Qty, string Status, DateTimeOffset PlacedAt);

class OrdersDatabase
{
    private readonly Dictionary<string, Order> _orders = new();

    public void Insert(Order o) { _orders[o.OrderId] = o; Console.WriteLine($"  [OrdersDB]     INSERT order {o.OrderId}"); }
    public Order? Find(string id) => _orders.GetValueOrDefault(id);
    public IReadOnlyList<Order> ByCustomer(string cid) =>
        _orders.Values.Where(o => o.CustomerId == cid).ToList();
}

class OrdersService(OrdersDatabase db)
{
    public Order Create(string customerId, string sku, int qty)
    {
        var order = new Order(
            OrderId: Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CustomerId: customerId,
            Sku: sku,
            Qty: qty,
            Status: "Placed",
            PlacedAt: DateTimeOffset.UtcNow);
        db.Insert(order);
        return order;
    }

    // Public API — cross-service access point
    public Order? GetById(string id) => db.Find(id);
}

// ── Inventory Service — owns its own inventory store ──────
record InventoryItem(string Sku, int Stock, int Reserved);

class InventoryDatabase
{
    private readonly Dictionary<string, InventoryItem> _items = new()
    {
        ["WIDGET-PRO"] = new("WIDGET-PRO", 100, 0),
        ["GADGET-X"] = new("GADGET-X", 50, 0),
    };

    public InventoryItem? Find(string sku) => _items.GetValueOrDefault(sku);
    public void Update(InventoryItem item) { _items[item.Sku] = item; Console.WriteLine($"  [InventoryDB]  UPDATE {item.Sku}: stock={item.Stock}, reserved={item.Reserved}"); }
}

class InventoryService(InventoryDatabase db)
{
    // Called via HTTP from OrdersService — NOT a direct DB join
    public bool Reserve(string sku, int qty)
    {
        var item = db.Find(sku);
        if (item is null || item.Stock - item.Reserved < qty)
        {
            Console.WriteLine($"  [InventoryService] Reserve FAILED: insufficient stock for {sku}");
            return false;
        }
        db.Update(item with { Reserved = item.Reserved + qty });
        return true;
    }

    // Public API
    public int Available(string sku)
    {
        var item = db.Find(sku);
        return item is null ? 0 : item.Stock - item.Reserved;
    }
}

// ── Placing an order: cross-service call via API ───────────
class OrderProcessingWorkflow(OrdersService orders, InventoryService inventory)
{
    public Order? PlaceOrder(string customerId, string sku, int qty)
    {
        Console.WriteLine($"\n[Workflow] Placing order: customer={customerId}, sku={sku}, qty={qty}");

        // Cross-service call — goes through API, never direct DB access
        bool reserved = inventory.Reserve(sku, qty);
        if (!reserved) return null;

        return orders.Create(customerId, sku, qty);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Database per Service ===\n");
Console.WriteLine("Each service owns its private store. Cross-service data = API calls.\n");

var ordersDb = new OrdersDatabase();
var inventoryDb = new InventoryDatabase();
var ordersService = new OrdersService(ordersDb);
var inventoryService = new InventoryService(inventoryDb);
var workflow = new OrderProcessingWorkflow(ordersService, inventoryService);

var order1 = workflow.PlaceOrder("CUST-1", "WIDGET-PRO", 3);
Console.WriteLine($"  Order created: {order1?.OrderId}\n");

Console.WriteLine($"Available stock (WIDGET-PRO): {inventoryService.Available("WIDGET-PRO")}");

var order2 = workflow.PlaceOrder("CUST-2", "WIDGET-PRO", 98);  // should fail
Console.WriteLine($"  Over-stock order result: {(order2 is null ? "REJECTED" : order2.OrderId)}");

Console.WriteLine("\nEach service's schema can evolve independently — zero coupling.");
