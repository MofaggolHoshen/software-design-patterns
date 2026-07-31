// ============================================================
// Facade Pattern — C# Example
// ============================================================
//
// Intent: Provide a simplified interface to a complex subsystem.
//
// Key roles:
//   InventoryService, PaymentGateway, ShippingService,
//   NotificationService, AuditLogger — Subsystem classes
//   OrderFacade    — Facade (single entry point)
// ============================================================

// ── Subsystem classes ─────────────────────────────────────
class InventoryService
{
    public bool CheckStock(int productId, int qty)
    {
        Console.WriteLine($"  [Inventory]    Stock check: product {productId} × {qty} — OK");
        return true;
    }
    public void Reserve(int productId, int qty) =>
        Console.WriteLine($"  [Inventory]    Reserved {qty} × product {productId}");
}

class PaymentGateway
{
    public string Charge(int customerId, decimal amount)
    {
        var payRef = $"PAY-{Guid.NewGuid():N}"[..12].ToUpper();
        Console.WriteLine($"  [Payment]      Charged {amount:C} for customer {customerId} → {payRef}");
        return payRef;
    }
}

class ShippingService
{
    public string Dispatch(int productId, string address)
    {
        var tracking = $"SHIP-{Guid.NewGuid():N}"[..10].ToUpper();
        Console.WriteLine($"  [Shipping]     Product {productId} → {address} | {tracking}");
        return tracking;
    }
}

class NotificationService
{
    public void SendConfirmation(string email, string trackingNumber) =>
        Console.WriteLine($"  [Notification] Email sent to {email} (tracking: {trackingNumber})");
}

class AuditLogger
{
    public void Log(string message) =>
        Console.WriteLine($"  [Audit]        {DateTime.UtcNow:HH:mm:ss.fff} {message}");
}

// ── Request DTO ───────────────────────────────────────────
record PlaceOrderRequest(
    int ProductId,
    int Quantity,
    int CustomerId,
    decimal Amount,
    string DeliveryAddress,
    string CustomerEmail);

// ── Facade ─────────────────────────────────────────────────
class OrderFacade(
    InventoryService inventory,
    PaymentGateway payment,
    ShippingService shipping,
    NotificationService notifier,
    AuditLogger logger)
{
    // One-call API hides all orchestration complexity from the client
    public string PlaceOrder(PlaceOrderRequest req)
    {
        logger.Log($"PlaceOrder START for customer {req.CustomerId}");

        if (!inventory.CheckStock(req.ProductId, req.Quantity))
            throw new InvalidOperationException("Product out of stock.");

        inventory.Reserve(req.ProductId, req.Quantity);

        var payRef = payment.Charge(req.CustomerId, req.Amount);
        var tracking = shipping.Dispatch(req.ProductId, req.DeliveryAddress);

        notifier.SendConfirmation(req.CustomerEmail, tracking);
        logger.Log($"PlaceOrder DONE — pay={payRef}, ship={tracking}");

        return tracking;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Facade Pattern ===\n");

var facade = new OrderFacade(
    new InventoryService(),
    new PaymentGateway(),
    new ShippingService(),
    new NotificationService(),
    new AuditLogger());

var tracking = facade.PlaceOrder(new PlaceOrderRequest(
    ProductId: 42,
    Quantity: 2,
    CustomerId: 1001,
    Amount: 59.98m,
    DeliveryAddress: "123 Main St, Springfield",
    CustomerEmail: "alice@example.com"));

Console.WriteLine($"\nOrder placed successfully. Tracking: {tracking}");
