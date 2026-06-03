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

Saga is commonly implemented in two styles:

## Choreography Based Saga

In a **Choreography Based Saga**, there is **no central controller**. Each service listens for domain events, performs its own local transaction, and publishes the next event. The flow emerges from event-to-event reactions.

### Choreography Flow

1. `Orders` creates the order and publishes `OrderCreated`.
2. `Inventory` listens, reserves stock, and publishes `InventoryReserved`.
3. `Payment` listens, charges the customer, and publishes `PaymentCharged`.
4. If payment fails, a compensation event such as `OrderCancelled` or `InventoryReleased` is published.

### Choreography Example

```csharp
// Choreography-based Saga: each service reacts to events.

interface IEvent { }
record OrderCreated(string OrderId, string Sku, int Qty, decimal Total) : IEvent;
record InventoryReserved(string OrderId) : IEvent;
record PaymentCharged(string OrderId, string ChargeId) : IEvent;
record PaymentFailed(string OrderId, string Reason) : IEvent;
record OrderCancelled(string OrderId, string Reason) : IEvent;

class OrdersSagaHandler
{
    public void Start(string orderId, string sku, int qty, decimal total)
    {
        Console.WriteLine($"[Orders] Created order {orderId}");
        // Publish: OrderCreated
    }

    public void On(PaymentCharged e)
    {
        Console.WriteLine($"[Orders] Confirmed order {e.OrderId}");
    }

    public void On(PaymentFailed e)
    {
        Console.WriteLine($"[Orders] Cancelled order {e.OrderId}: {e.Reason}");
        // Publish: OrderCancelled
    }
}

class InventorySagaHandler
{
    public void On(OrderCreated e)
    {
        Console.WriteLine($"[Inventory] Reserved {e.Qty} x {e.Sku} for {e.OrderId}");
        // Publish: InventoryReserved
    }

    public void On(OrderCancelled e)
    {
        Console.WriteLine($"[Inventory] Compensation: released stock for {e.OrderId}");
    }
}

class PaymentSagaHandler
{
    public void On(InventoryReserved e)
    {
        Console.WriteLine($"[Payment] Charging customer for {e.OrderId}");
        // Publish: PaymentCharged or PaymentFailed
    }
}
```

### When Choreography Fits

- Best when services are already event-driven.
- Good for loose coupling between teams and services.
- Harder to trace because the workflow logic is spread across services.

## Orchestration Based Saga

In an **Orchestration Based Saga**, a dedicated **Saga Orchestrator** controls the workflow. Instead of each service deciding the next step on its own, the orchestrator sends commands, waits for replies/events, updates saga state, and triggers compensations when needed.

### Orchestration Flow

1. The orchestrator receives `PlaceOrder`.
2. It tells `Inventory` to reserve stock.
3. If stock is reserved, it tells `Payment` to charge the customer.
4. If payment succeeds, it tells `Orders` to confirm the order.
5. If payment fails, it tells `Inventory` to release the reservation and tells `Orders` to cancel the order.

### Orchestration Example

```csharp
// Orchestration-based Saga: a central orchestrator drives each step.

record PlaceOrder(string OrderId, string Sku, int Qty, decimal Total);
record ReserveInventory(string OrderId, string Sku, int Qty);
record ChargePayment(string OrderId, decimal Total);
record ReleaseInventory(string OrderId);
record ConfirmOrder(string OrderId);
record CancelOrder(string OrderId, string Reason);

class OrderSagaOrchestrator
{
    public async Task HandleAsync(PlaceOrder cmd)
    {
        Console.WriteLine($"[Saga] Starting saga for {cmd.OrderId}");

        bool inventoryReserved = await ReserveInventoryAsync(cmd.OrderId, cmd.Sku, cmd.Qty);
        if (!inventoryReserved)
        {
            await CancelOrderAsync(cmd.OrderId, "Out of stock");
            return;
        }

        bool paymentCharged = await ChargePaymentAsync(cmd.OrderId, cmd.Total);
        if (!paymentCharged)
        {
            await ReleaseInventoryAsync(cmd.OrderId); // compensation
            await CancelOrderAsync(cmd.OrderId, "Payment failed");
            return;
        }

        await ConfirmOrderAsync(cmd.OrderId);
    }

    private Task<bool> ReserveInventoryAsync(string orderId, string sku, int qty)
    {
        Console.WriteLine($"[Saga] Command -> Inventory: reserve {qty} x {sku}");
        return Task.FromResult(true);
    }

    private Task<bool> ChargePaymentAsync(string orderId, decimal total)
    {
        Console.WriteLine($"[Saga] Command -> Payment: charge {total:C}");
        return Task.FromResult(true);
    }

    private Task ReleaseInventoryAsync(string orderId)
    {
        Console.WriteLine($"[Saga] Command -> Inventory: release reservation for {orderId}");
        return Task.CompletedTask;
    }

    private Task ConfirmOrderAsync(string orderId)
    {
        Console.WriteLine($"[Saga] Command -> Orders: confirm {orderId}");
        return Task.CompletedTask;
    }

    private Task CancelOrderAsync(string orderId, string reason)
    {
        Console.WriteLine($"[Saga] Command -> Orders: cancel {orderId} ({reason})");
        return Task.CompletedTask;
    }
}
```

### When Orchestration Fits

- Best when the workflow is complex and needs a clear owner.
- Easier to monitor, debug, and change because the process is centralized.
- Introduces a central coordinator that must be reliable and scalable.

## Key Takeaways

- **Choreography Based Saga**: services collaborate by publishing and consuming events; control flow is decentralized.
- **Orchestration Based Saga**: a central saga coordinator tells each participant what to do next; control flow is explicit.
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
