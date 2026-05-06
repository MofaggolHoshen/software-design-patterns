// ============================================================
// Saga — C# Simulation (Choreography-based)
// ============================================================
//
// Intent: Manage a distributed business transaction as a
// sequence of local service transactions. Each step emits
// an event; subsequent services react. On failure, compensating
// transactions undo previous steps in reverse order.
//
// Simulates the Order Placement Saga:
//   1. OrdersService   creates order
//   2. InventoryService reserves stock   (compensate: release)
//   3. PaymentService  charges customer  (compensate: refund)
//   4. OrdersService   confirms order
// ============================================================

// ── Domain events ─────────────────────────────────────────
interface IDomainEvent { string OrderId { get; } }
record OrderCreated(string OrderId, string Sku, int Qty, decimal Amount) : IDomainEvent;
record InventoryReserved(string OrderId, string ReservationId) : IDomainEvent;
record InventoryFailed(string OrderId, string Reason) : IDomainEvent;
record PaymentCharged(string OrderId, string ChargeId) : IDomainEvent;
record PaymentFailed(string OrderId, string Reason) : IDomainEvent;
record InventoryReleased(string OrderId) : IDomainEvent;
record OrderConfirmed(string OrderId) : IDomainEvent;
record OrderCancelled(string OrderId, string Reason) : IDomainEvent;

// ── Minimal event bus ──────────────────────────────────────
class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : IDomainEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) _handlers[typeof(T)] = list = new();
        list.Add(handler);
    }

    public void Publish<T>(T ev) where T : IDomainEvent
    {
        Console.WriteLine($"  [Bus] Published {typeof(T).Name} for order {ev.OrderId}");
        if (_handlers.TryGetValue(typeof(T), out var handlers))
            foreach (var h in handlers) h.DynamicInvoke(ev);
    }
}

// ── Saga participants ──────────────────────────────────────
class OrdersSagaParticipant(EventBus bus)
{
    private readonly Dictionary<string, string> _orders = new();

    public void CreateOrder(string sku, int qty, decimal amount)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        _orders[orderId] = "Created";
        bus.Publish(new OrderCreated(orderId, sku, qty, amount));
    }

    public void On(PaymentCharged e)
    {
        _orders[e.OrderId] = "Confirmed";
        Console.WriteLine($"    [Orders] Order {e.OrderId} CONFIRMED (charge={e.ChargeId})");
        bus.Publish(new OrderConfirmed(e.OrderId));
    }

    public void On(PaymentFailed e)
    {
        _orders[e.OrderId] = "Cancelled";
        Console.WriteLine($"    [Orders] Order {e.OrderId} CANCELLED — {e.Reason}");
        bus.Publish(new OrderCancelled(e.OrderId, e.Reason));
    }
}

class InventorySagaParticipant(EventBus bus, bool simulateFailure = false)
{
    public void On(OrderCreated e)
    {
        if (simulateFailure)
        {
            Console.WriteLine($"    [Inventory] Failed to reserve for order {e.OrderId}");
            bus.Publish(new InventoryFailed(e.OrderId, "Out of stock"));
            return;
        }
        var resId = $"RES-{e.OrderId}";
        Console.WriteLine($"    [Inventory] Reserved {e.Qty}×{e.Sku} → {resId}");
        bus.Publish(new InventoryReserved(e.OrderId, resId));
    }

    public void On(OrderCancelled e)
    {
        Console.WriteLine($"    [Inventory] COMPENSATION: Released reservation for {e.OrderId}");
        bus.Publish(new InventoryReleased(e.OrderId));
    }
}

class PaymentSagaParticipant(EventBus bus, bool simulateFailure = false)
{
    public void On(InventoryReserved e)
    {
        if (simulateFailure)
        {
            Console.WriteLine($"    [Payment] Charge failed for order {e.OrderId}");
            bus.Publish(new PaymentFailed(e.OrderId, "Card declined"));
            return;
        }
        var chargeId = $"CHG-{e.OrderId}";
        Console.WriteLine($"    [Payment] Charged → {chargeId}");
        bus.Publish(new PaymentCharged(e.OrderId, chargeId));
    }
}

void WireUpSaga(EventBus bus, bool inventoryFail, bool paymentFail)
{
    var orders = new OrdersSagaParticipant(bus);
    var inventory = new InventorySagaParticipant(bus, inventoryFail);
    var payment = new PaymentSagaParticipant(bus, paymentFail);

    bus.Subscribe<OrderCreated>(inventory.On);
    bus.Subscribe<InventoryReserved>(payment.On);
    bus.Subscribe<PaymentCharged>(orders.On);
    bus.Subscribe<PaymentFailed>(orders.On);
    bus.Subscribe<OrderCancelled>(inventory.On);

    orders.CreateOrder("WIDGET-PRO", 2, 59.98m);
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Saga (Choreography) ===\n");

Console.WriteLine("--- Happy path ---");
WireUpSaga(new EventBus(), inventoryFail: false, paymentFail: false);

Console.WriteLine("\n--- Payment failure with compensation ---");
WireUpSaga(new EventBus(), inventoryFail: false, paymentFail: true);
// Inventory reservation is released as compensation

Console.WriteLine("\n--- Inventory failure ---");
WireUpSaga(new EventBus(), inventoryFail: true, paymentFail: false);
