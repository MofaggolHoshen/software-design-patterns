# 🏢 Decomposition by Business Capabilities

The first question when adopting microservices is "how big should a service be?" **Decomposition by Business Capabilities** answers this by drawing service boundaries along stable, organisationally-aligned capabilities — the things the business does — rather than along technical layers or individual entities.

## Intent

> Define a service boundary for each distinct business capability; a business capability is a stable function the organisation performs (e.g., Ordering, Payments, Shipping) that has its own data, rules, and people.

## Problem

Teams that decompose services by technical layer (UI, API, Database) or by CRUD entity create services that are tightly coupled through shared business logic. A change to the "Order" concept touches every service, requiring synchronised releases across teams and causing the dreaded distributed monolith.

### Bad Example

```csharp
// Technical-layer decomposition — all business logic in one "API" service
// Change "order status" → must redeploy API, not just the Orders service
class MonolithicApiService
{
    // Handles orders, inventory, payments, shipping all in one place
    public async Task PlaceOrderAsync(OrderRequest req)
    {
        // All business logic is here — impossible to evolve independently
        await _db.SaveOrderAsync(req);
        await _db.DeductInventoryAsync(req.ProductId, req.Quantity);
        await _stripeClient.ChargeAsync(req.CustomerId, req.Amount);
        await _shippingApi.CreateShipmentAsync(req);
    }
}
```

### Good Example

```csharp
// Each business capability is an independent service with its own API
// -----------------------------------------------------------------
// OrdersService — owns order lifecycle (created, confirmed, cancelled)
// InventoryService — owns stock levels
// PaymentService — owns charging and refunds
// ShipmentService — owns dispatching and tracking

// Services communicate via events or direct calls, never shared DB
interface IOrdersService
{
    Task<string> CreateOrderAsync(CreateOrderRequest req);
    Task<OrderStatus> GetStatusAsync(string orderId);
}

interface IInventoryService
{
    Task<bool>  ReserveAsync(string sku, int quantity);
    Task        ReleaseAsync(string reservationId);
}

interface IPaymentService
{
    Task<string> ChargeAsync(string customerId, decimal amount);
    Task         RefundAsync(string chargeId);
}

interface IShipmentService
{
    Task<string> DispatchAsync(string orderId, Address destination);
}

// The orchestrator (e.g., an Order Saga) calls each service in turn.
// A change to payment logic only requires redeploying PaymentService.
```

## Key Takeaways

- Business capabilities map to stable **organisational units** (teams, departments). They change rarely.
- Each service owns its data — no two services share a database table.
- Conway's Law: the software structure should mirror (or deliberately deviate from) the team structure.
- Start with coarse-grained services and split only when a capability grows large enough to warrant its own deployment.

## When to Use

- Breaking a monolith apart: identify the capabilities first, then draw service boundaries.
- Aligning teams — "two-pizza rule": each service should be ownable by one small team.
- When different capabilities have very different scaling, availability, or release-cadence requirements.

## When NOT to Use

- Small applications or teams: microservices add network, deployment, and operational overhead that outweighs the benefits.
- When the business capabilities are not yet well understood — premature decomposition creates the wrong services.
- If your organisation cannot support independent CI/CD — all services will still be deployed together.
