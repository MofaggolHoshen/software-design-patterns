# 📜 Saga

Distributed transactions (2PC) block resources and couple services tightly. The **Saga** pattern implements a long-running business transaction as a sequence of local service transactions, each publishing an event or calling the next step. If any step fails, **compensating transactions** are executed in reverse to undo the changes already made.

## Intent

> Manage distributed transactions as a sequence of local service transactions with corresponding compensating actions for rollback, ensuring the overall business process reaches a consistent final state without distributed locking.

## Problem

Placing an order involves `Orders`, `Inventory`, `Payment`, and `Shipping` services. A single 2PC transaction across all four requires them to lock resources and coordinate a commit — coupling them tightly, blocking resources, and failing catastrophically if any coordinator crashes.

### Bad Example

```csharp
// 2-Phase Commit across services — tight coupling, blocking, fragile
class OrderCoordinator2PC
{
    async Task PlaceOrderAsync(OrderRequest req)
    {
        // Phase 1: Prepare (lock resources in all services)
        await _orders.PrepareAsync(req);      // locks Order table
        await _inventory.PrepareAsync(req);   // locks Inventory row
        await _payment.PrepareAsync(req);     // locks Payment row
        // If coordinator crashes here, all services remain locked forever

        // Phase 2: Commit (or abort all)
        await _orders.CommitAsync();
        await _inventory.CommitAsync();
        await _payment.CommitAsync();
    }
}
```

### Good Example

```csharp
// ── Choreography-based Saga (event-driven) ────────────────
// Each service reacts to events and emits the next event.
// Compensations are emitted when a step fails.

interface IEvent { }
record OrderCreated    (string OrderId, string Sku, int Qty, decimal Total) : IEvent;
record InventoryReserved(string OrderId) : IEvent;
record InventoryFailed  (string OrderId, string Reason) : IEvent;
record PaymentCharged   (string OrderId, string ChargeId) : IEvent;
record PaymentFailed    (string OrderId, string Reason) : IEvent;
record InventoryReleased(string OrderId) : IEvent;   // compensating event

// OrdersService: creates order, listens for PaymentCharged/Failed
class OrdersSagaHandler
{
    public void On(PaymentCharged e)
    {
        Console.WriteLine($"  [Orders] Order {e.OrderId} confirmed (charge {e.ChargeId}).");
        // Publish: OrderConfirmed
    }
    public void On(PaymentFailed e)
    {
        Console.WriteLine($"  [Orders] Order {e.OrderId} cancelled — payment failed.");
        // Publish: OrderCancelled (compensation)
    }
}

// InventoryService: reserves on OrderCreated, releases on OrderCancelled
class InventorySagaHandler
{
    public void On(OrderCreated e)
    {
        bool ok = true; // simulate reserve
        if (ok) Console.WriteLine($"  [Inventory] Reserved {e.Qty} × {e.Sku} for {e.OrderId}.");
        // Emit: InventoryReserved or InventoryFailed
    }
    public void On(InventoryReleased e)
    {
        Console.WriteLine($"  [Inventory] Released reservation for {e.OrderId}."); // compensation
    }
}

// PaymentService: charges on InventoryReserved, publishes outcome
class PaymentSagaHandler
{
    public void On(InventoryReserved e)
    {
        Console.WriteLine($"  [Payment] Charging for order {e.OrderId}...");
        // Emit: PaymentCharged or PaymentFailed
        // On failure also emit: InventoryReleased (trigger compensation)
    }
}
```

## Key Takeaways

- **Choreography**: each service reacts to events from the broker — simple, decoupled, but flows are implicit.
- **Orchestration**: a central Saga Orchestrator (state machine) drives each step explicitly — easier to observe and debug.
- Compensating transactions must be **idempotent** — they may be retried.
- Data is eventually consistent: intermediate states (inventory reserved, payment pending) are visible to observers.
- MassTransit and NServiceBus provide saga state machine frameworks for .NET.

## When to Use

- Multi-step business transactions that span two or more services with independent databases.
- When 2PC is not an option (different databases, cloud services, external APIs).
- Long-running workflows (order fulfilment, onboarding) where steps may take seconds to hours.

## When NOT to Use

- When operations can be redesigned to fit within a single service's local transaction — prefer that.
- When the compensation logic is too complex to implement correctly — simplify the workflow first.
- If strict isolation is required between steps — Saga provides ACI without the D; consider if that is acceptable.
