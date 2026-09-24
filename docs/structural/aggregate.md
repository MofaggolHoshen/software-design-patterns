# 📦 Aggregate Pattern

The Aggregate pattern (from Domain-Driven Design) groups related objects into a **cluster with a single root entity** that controls all access to the cluster. External objects may only reference the aggregate root — never the internal entities or value objects directly.

## Intent

> Group related domain objects into a cluster with a well-defined boundary. Designate one entity as the **Aggregate Root** that controls all access and enforces invariants for the entire cluster.

## Problem

When multiple entities are closely related (e.g., an `Order` and its `OrderLine` items), allowing direct external references to inner objects breaks encapsulation of the cluster. External code can put the aggregate into an inconsistent state by adding lines that exceed a limit, or by modifying a line when the order is already shipped.

### Bad Example

```csharp
class OrderLine
{
    public int    ProductId { get; set; }
    public int    Quantity  { get; set; }
    public decimal Price     { get; set; }
}

class Order
{
    public int           Id    { get; set; }
    public List<OrderLine> Lines { get; set; } = new(); // publicly mutable!
}

// External code bypasses Order's rules
var order = new Order();
order.Lines.Add(new OrderLine { Quantity = -5, Price = 9.99m }); // negative qty allowed!
order.Lines.Add(new OrderLine { Quantity = 1000, Price = 0.01m }); // fraud-risk allowed!
```

### Good Example

```csharp
// ── Value Object (immutable) ──────────────────────────────
record Money(decimal Amount, string Currency)
{
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException("Currency mismatch.");
        return new Money(a.Amount + b.Amount, a.Currency);
    }
}

// ── Inner Entity — only accessible through the root ───────
class OrderLine
{
    public int   ProductId { get; }
    public int   Quantity  { get; private set; }
    public Money UnitPrice { get; }

    internal OrderLine(int productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        ProductId = productId;
        Quantity  = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    internal void UpdateQuantity(int qty)
    {
        if (qty <= 0) throw new ArgumentException("Quantity must be positive.");
        Quantity = qty;
    }
}

// ── Aggregate Root ────────────────────────────────────────
class Order
{
    private readonly List<OrderLine> _lines = new();
    public  IReadOnlyList<OrderLine> Lines  => _lines.AsReadOnly();

    public int    Id     { get; }
    public string Status { get; private set; } = "Pending";

    public Order(int id) => Id = id;

    // All mutations go through the root — invariants enforced here
    public void AddLine(int productId, int quantity, Money unitPrice)
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Cannot modify a non-pending order.");
        if (_lines.Count >= 50)
            throw new InvalidOperationException("Order cannot exceed 50 lines.");
        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void Ship()
    {
        if (!_lines.Any()) throw new InvalidOperationException("Cannot ship an empty order.");
        Status = "Shipped";
        Console.WriteLine($"  Order {Id} shipped with {_lines.Count} line(s).");
    }

    public Money Total => _lines.Aggregate(new Money(0, "USD"), (sum, l) => sum + l.LineTotal);
}

// ── Demo ──────────────────────────────────────────────────
var order = new Order(1001);
order.AddLine(42, 2, new Money(29.99m, "USD"));
order.AddLine(99, 1, new Money(14.50m, "USD"));
Console.WriteLine($"  Total: {order.Total.Amount:C} {order.Total.Currency}");
order.Ship();

try { order.AddLine(1, 1, new Money(5m, "USD")); }
catch (Exception ex) { Console.WriteLine($"  Caught: {ex.Message}"); }
```

## Key Takeaways

- The aggregate root is the only public entry point — external code has no references to inner entities.
- All business invariants are enforced by the root, preventing inconsistent states.
- Repositories load and save complete aggregates, never partial inner entities.
- Keep aggregates small — large aggregates cause contention and scalability issues.

## When to Use

- A group of domain objects must enforce shared invariants (e.g., an order total limit).
- You want to prevent external code from putting a cluster of objects into an invalid state.
- You are modelling a domain with DDD and need clear transactional boundaries.

## When NOT to Use

- The objects are independent and share no invariants — model them as separate entities.
- CRUD applications with no domain logic — aggregates add unnecessary overhead.
