// ============================================================
// Decomposition by Business Capabilities — C# Simulation
// ============================================================
//
// Intent: Define a microservice boundary for each distinct
// business capability (Orders, Inventory, Payments, Shipping)
// so each can be independently developed and deployed.
//
// Key roles:
//   IOrdersService, IInventoryService, IPaymentService,
//   IShippingService — one interface per capability
//   OrderOrchestrator — coordinates the place-order workflow
// ============================================================

// ── Each capability is an independent service ─────────────
record CreateOrderRequest(string CustomerId, string Sku, int Quantity, decimal Price);
record OrderDto(string OrderId, string Status);
record ReservationDto(string ReservationId, string Sku, int Quantity);
record ChargeDto(string ChargeId, decimal Amount);
record ShipmentDto(string TrackingNumber);

interface IOrdersService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest req);
    Task ConfirmAsync(string orderId);
    Task CancelAsync(string orderId);
}

interface IInventoryService
{
    Task<ReservationDto> ReserveAsync(string sku, int qty);
    Task ReleaseAsync(string reservationId);
}

interface IPaymentService
{
    Task<ChargeDto> ChargeAsync(string customerId, decimal amount);
    Task RefundAsync(string chargeId);
}

interface IShippingService
{
    Task<ShipmentDto> DispatchAsync(string orderId, string customerId);
}

// ── Stub implementations (simulate each microservice) ──────
class OrdersService : IOrdersService
{
    public Task<OrderDto> CreateAsync(CreateOrderRequest req)
    {
        var id = Guid.NewGuid().ToString("N")[..8].ToUpper();
        Console.WriteLine($"  [Orders]    Created order {id}");
        return Task.FromResult(new OrderDto(id, "Pending"));
    }
    public Task ConfirmAsync(string id) { Console.WriteLine($"  [Orders]    Confirmed {id}"); return Task.CompletedTask; }
    public Task CancelAsync(string id) { Console.WriteLine($"  [Orders]    Cancelled {id}"); return Task.CompletedTask; }
}

class InventoryService : IInventoryService
{
    public Task<ReservationDto> ReserveAsync(string sku, int qty)
    {
        var id = $"RES-{Guid.NewGuid():N}"[..12].ToUpper();
        Console.WriteLine($"  [Inventory] Reserved {qty}×{sku} → {id}");
        return Task.FromResult(new ReservationDto(id, sku, qty));
    }
    public Task ReleaseAsync(string id) { Console.WriteLine($"  [Inventory] Released {id}"); return Task.CompletedTask; }
}

class PaymentService : IPaymentService
{
    public Task<ChargeDto> ChargeAsync(string customerId, decimal amount)
    {
        var id = $"CHG-{Guid.NewGuid():N}"[..12].ToUpper();
        Console.WriteLine($"  [Payment]   Charged {amount:C} for {customerId} → {id}");
        return Task.FromResult(new ChargeDto(id, amount));
    }
    public Task RefundAsync(string id) { Console.WriteLine($"  [Payment]   Refunded {id}"); return Task.CompletedTask; }
}

class ShippingService : IShippingService
{
    public Task<ShipmentDto> DispatchAsync(string orderId, string customerId)
    {
        var tn = $"SHIP-{Guid.NewGuid():N}"[..12].ToUpper();
        Console.WriteLine($"  [Shipping]  Dispatched order {orderId} → {tn}");
        return Task.FromResult(new ShipmentDto(tn));
    }
}

// ── Orchestrator — coordinates the place-order workflow ────
class OrderOrchestrator(
    IOrdersService orders,
    IInventoryService inventory,
    IPaymentService payments,
    IShippingService shipping)
{
    public async Task<string> PlaceOrderAsync(CreateOrderRequest req)
    {
        var order = await orders.CreateAsync(req);
        var reservation = await inventory.ReserveAsync(req.Sku, req.Quantity);

        ChargeDto charge;
        try
        {
            charge = await payments.ChargeAsync(req.CustomerId, req.Price);
        }
        catch
        {
            await inventory.ReleaseAsync(reservation.ReservationId);
            await orders.CancelAsync(order.OrderId);
            throw;
        }

        var shipment = await shipping.DispatchAsync(order.OrderId, req.CustomerId);
        await orders.ConfirmAsync(order.OrderId);

        return shipment.TrackingNumber;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Decomposition by Business Capabilities ===\n");
Console.WriteLine("Each service is independently deployable and owns its own data.\n");

var orchestrator = new OrderOrchestrator(
    new OrdersService(), new InventoryService(),
    new PaymentService(), new ShippingService());

var tracking = await orchestrator.PlaceOrderAsync(new CreateOrderRequest(
    CustomerId: "CUST-42",
    Sku: "WIDGET-PRO",
    Quantity: 3,
    Price: 29.97m));

Console.WriteLine($"\nOrder placed. Tracking number: {tracking}");
Console.WriteLine("\nEach service can be scaled, deployed, and owned independently.");
