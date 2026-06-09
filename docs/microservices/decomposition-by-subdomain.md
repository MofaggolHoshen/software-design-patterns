# 🗺️ Decomposition by Subdomain

While **Decomposition by Business Capabilities** uses organisational boundaries, **Decomposition by Subdomain** uses Domain-Driven Design (DDD) to identify **Bounded Contexts** — linguistically and logically coherent regions of the domain model — as the unit of service decomposition.

## Intent

> Use DDD Bounded Contexts to identify where the ubiquitous language changes and the domain model diverges, then draw service boundaries there — ensuring each service has an internally coherent model without translation friction.

## Problem

A large domain shares terms that mean different things in different contexts. "Customer" means a contact record in CRM, a billing account in Payments, and a recipient address in Shipping. Placing all three meanings in one service forces awkward compromises; the `Customer` object becomes a god object satisfying no context well.

### Bad Example

```csharp
// One Customer class forced to satisfy all contexts — God Object
class Customer
{
    // CRM context
    public string Name    { get; set; }
    public string Email   { get; set; }
    public string Phone   { get; set; }
    public string Segment { get; set; }

    // Billing context
    public string  PaymentMethod  { get; set; }
    public bool    HasValidCard   { get; set; }
    public decimal OutstandingBalance { get; set; }

    // Shipping context
    public string StreetAddress { get; set; }
    public string City          { get; set; }
    public string PostalCode    { get; set; }

    // Every team changes this class; conflicts are constant
}
```

### Good Example

```csharp
// ── CRM Bounded Context ────────────────────────────────
namespace Crm
{
    record Contact(Guid Id, string Name, string Email, string Segment);

    interface IContactService
    {
        Task<Contact>  FindAsync(Guid id);
        Task           UpdateSegmentAsync(Guid id, string segment);
    }
}

// ── Billing Bounded Context ────────────────────────────
namespace Billing
{
    record BillingAccount(Guid CustomerId, string PaymentMethodToken, decimal Balance);

    interface IBillingService
    {
        Task<string>   ChargeAsync(Guid customerId, decimal amount);
        Task           RefundAsync(string chargeId);
    }
}

// ── Shipping Bounded Context ──────────────────────────
namespace Shipping
{
    record Recipient(Guid OrderId, string FullName, string Street,
                     string City, string PostalCode, string Country);

    interface IShippingService
    {
        Task<string> DispatchAsync(Recipient to, string sku);
    }
}

// Each context has its own Customer concept, its own ubiquitous language,
// and its own service. Translation happens at integration points (ACL).
```

## Key Takeaways

- A **Bounded Context** is the primary DDD unit of service decomposition.
- Each context has its own **ubiquitous language** — terms mean exactly one thing within the context.
- **Anti-Corruption Layers (ACL)** translate between contexts at integration points.
- Subdomain types: **Core** (your competitive advantage, invest here), **Supporting** (necessary but not differentiating, build simply), **Generic** (commodity, buy/use a SaaS solution).

## When to Use

- Complex domains where the same term has different meanings across teams.
- When modelling a legacy monolith for strangler-fig decomposition.
- Teams practicing DDD and Event Storming to map domain events to bounded contexts.

## When NOT to Use

- Simple CRUD applications where a single model serves all contexts — DDD overhead isn't justified.
- If the domain is not yet well-understood — spend time on domain modelling before drawing service boundaries.
- When a single small team owns the entire domain — bounded contexts add complexity without the communication benefits.
