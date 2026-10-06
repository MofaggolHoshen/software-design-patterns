# 🏛️ Facade Pattern

The Facade pattern provides a **simplified interface** to a complex subsystem. It defines a higher-level interface that makes the subsystem easier to use, while keeping the subsystem available for clients who need fine-grained access.

## Intent

> Provide a unified interface to a set of interfaces in a subsystem. Facade defines a higher-level interface that makes the subsystem easier to use.

## Problem

Complex subsystems with many classes and dependencies are hard to use correctly. Clients must know the right order to call methods, which classes to instantiate, and how the pieces interact. This couples clients to implementation details; changing the subsystem breaks all clients.

### Bad Example

```csharp
// Client must orchestrate 5 subsystem classes in the right order — tight coupling
class OrderController
{
    public void PlaceOrder(OrderRequest req)
    {
        var inventory = new InventoryService();
        var payment   = new PaymentGateway();
        var shipper   = new ShippingService();
        var notifier  = new NotificationService();
        var logger    = new AuditLogger();

        if (!inventory.CheckStock(req.ProductId, req.Quantity))
            throw new Exception("Out of stock");
        logger.Log($"Stock checked for {req.ProductId}");

        var paymentRef = payment.Charge(req.CustomerId, req.Amount);
        logger.Log($"Payment {paymentRef} captured");

        var trackingNo = shipper.Ship(req.ProductId, req.Address);
        logger.Log($"Shipment {trackingNo} created");

        notifier.SendConfirmation(req.CustomerEmail, trackingNo);
        // Client handles the full orchestration — violates SRP
    }
}
```

### Good Example

```csharp
// ── Subsystem classes ──────────────────────────────────────
class InventoryService
{
    public bool CheckStock(int productId, int qty)
    {
        Console.WriteLine($"  [Inventory] Stock checked for product {productId} × {qty}");
        return true;
    }
    public void Reserve(int productId, int qty) =>
        Console.WriteLine($"  [Inventory] Reserved {qty} units of product {productId}");
}

class PaymentGateway
{
    public string Charge(int customerId, decimal amount)
    {
        var ref_ = $"PAY-{Guid.NewGuid():N}"[..12];
        Console.WriteLine($"  [Payment]   Charged {amount:C} for customer {customerId} → {ref_}");
        return ref_;
    }
}

class ShippingService
{
    public string Ship(int productId, string address)
    {
        var tracking = $"SHIP-{Guid.NewGuid():N}"[..10];
        Console.WriteLine($"  [Shipping]  Dispatched product {productId} to {address} → {tracking}");
        return tracking;
    }
}

class NotificationService
{
    public void SendConfirmation(string email, string trackingNo) =>
        Console.WriteLine($"  [Email]     Confirmation sent to {email} (tracking: {trackingNo})");
}

class AuditLogger
{
    public void Log(string message) =>
        Console.WriteLine($"  [Audit]     {DateTime.UtcNow:HH:mm:ss} {message}");
}

// ── Facade ─────────────────────────────────────────────────
record OrderRequest(int ProductId, int Quantity, int CustomerId,
                    decimal Amount, string Address, string CustomerEmail);

class OrderFacade(
    InventoryService    inventory,
    PaymentGateway      payment,
    ShippingService     shipping,
    NotificationService notifier,
    AuditLogger         logger)
{
    // One-call API hides all the orchestration complexity
    public string PlaceOrder(OrderRequest req)
    {
        if (!inventory.CheckStock(req.ProductId, req.Quantity))
            throw new InvalidOperationException("Out of stock.");

        inventory.Reserve(req.ProductId, req.Quantity);
        var payRef    = payment.Charge(req.CustomerId, req.Amount);
        var tracking  = shipping.Ship(req.ProductId, req.Address);
        notifier.SendConfirmation(req.CustomerEmail, tracking);
        logger.Log($"Order complete — payment={payRef}, tracking={tracking}");
        return tracking;
    }
}

// ── Demo ──────────────────────────────────────────────────
Console.WriteLine("=== Facade Pattern ===\n");

var facade = new OrderFacade(
    new InventoryService(),
    new PaymentGateway(),
    new ShippingService(),
    new NotificationService(),
    new AuditLogger());

var tracking = facade.PlaceOrder(new OrderRequest(
    ProductId: 42, Quantity: 2, CustomerId: 1001,
    Amount: 59.98m, Address: "123 Main St", CustomerEmail: "alice@example.com"));

Console.WriteLine($"\nOrder placed. Tracking: {tracking}");
```

## Key Takeaways

- Clients call one simple method instead of orchestrating many subsystem classes.
- The subsystem is not hidden — advanced clients can still bypass the facade and use subsystem classes directly.
- Facades are ideal for layered architecture: the Application Service layer is often a facade over the Domain layer.
- Multiple focused facades are better than one huge god facade.

## When to Use

- A complex subsystem needs a simple, task-oriented interface.
- You want to reduce dependencies by defining a clean entry point into a layer.
- You are wrapping a legacy API to provide a modern, readable interface.

## When NOT to Use

- The facade adds no simplification — it just re-exposes the subsystem 1:1.
- Clients need fine-grained control over every subsystem step.
