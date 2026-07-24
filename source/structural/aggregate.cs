// ============================================================
// Aggregate Pattern (DDD) — C# Example
// ============================================================
//
// Intent: Group related objects under a root entity that controls
// all access and enforces invariants for the entire cluster.
//
// Key roles:
//   Money       — Value Object (immutable)
//   OrderLine   — Inner Entity (accessible only through Order)
//   Order       — Aggregate Root
// ============================================================

// ── Value Object ──────────────────────────────────────────
record Money(decimal Amount, string Currency)
{
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Currency mismatch.");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}

// ── Inner Entity — constructor is internal so only Order creates it
class OrderLine
{
    public int ProductId { get; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; }

    internal OrderLine(int productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    internal void UpdateQuantity(int qty)
    {
        if (qty <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(qty));
        Quantity = qty;
    }

    public override string ToString() =>
        $"  Product {ProductId} × {Quantity} @ {UnitPrice} = {LineTotal}";
}

// ── Aggregate Root ────────────────────────────────────────
class Order
{
    private readonly List<OrderLine> _lines = new();

    public int Id { get; }
    public string Status { get; private set; } = "Pending";
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public Money Total => _lines
        .Aggregate(new Money(0, "USD"), (sum, l) => sum + l.LineTotal);

    public Order(int id) => Id = id;

    // All mutations go through the root — invariants enforced once, here
    public void AddLine(int productId, int quantity, Money unitPrice)
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Cannot modify a non-pending order.");
        if (_lines.Count >= 50)
            throw new InvalidOperationException("Order cannot exceed 50 lines.");
        _lines.Add(new OrderLine(productId, quantity, unitPrice));
        Console.WriteLine($"  Added line: Product {productId} × {quantity}");
    }

    public void UpdateLineQuantity(int productId, int newQty)
    {
        var line = _lines.FirstOrDefault(l => l.ProductId == productId)
            ?? throw new InvalidOperationException($"Product {productId} not found.");
        line.UpdateQuantity(newQty);
    }

    public void Ship()
    {
        if (!_lines.Any()) throw new InvalidOperationException("Cannot ship empty order.");
        Status = "Shipped";
        Console.WriteLine($"  Order {Id} shipped ({_lines.Count} lines, total {Total}).");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Aggregate Pattern ===\n");

var order = new Order(1001);
order.AddLine(10, 2, new Money(29.99m, "USD"));
order.AddLine(20, 1, new Money(14.50m, "USD"));
order.AddLine(30, 5, new Money(4.99m, "USD"));

foreach (var line in order.Lines) Console.WriteLine(line);
Console.WriteLine($"  Order total: {order.Total}");

Console.WriteLine("\nUpdating quantity of product 10:");
order.UpdateLineQuantity(10, 3);
Console.WriteLine($"  New total: {order.Total}");

Console.WriteLine("\nShipping order:");
order.Ship();

Console.WriteLine("\nAttempting to modify shipped order:");
try { order.AddLine(99, 1, new Money(9.99m, "USD")); }
catch (Exception ex) { Console.WriteLine($"  Caught: {ex.Message}"); }
