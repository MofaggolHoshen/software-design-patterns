# 🔌 Adapter Pattern

The Adapter pattern converts the **interface of a class** into another interface that clients expect. It allows classes with incompatible interfaces to work together without modifying their source code.

## Intent

> Convert the interface of a class into another interface clients expect. Adapter lets classes work together that couldn't otherwise because of incompatible interfaces.

## Problem

When integrating a third-party library, a legacy component, or an external service whose interface doesn't match what your code expects, you either break encapsulation by changing the client, or you change the external code (which you may not own). Both approaches are fragile.

### Bad Example

```csharp
// Third-party payment library with an incompatible interface
class StripePaymentGateway
{
    public bool ChargeCard(string cardToken, int amountInCents)
    {
        Console.WriteLine($"Stripe: charged {amountInCents} cents");
        return true;
    }
}

// Our app expects IPaymentProcessor — we can't change the third-party class
class OrderService
{
    // Forced to depend directly on Stripe — tight coupling, hard to swap
    private readonly StripePaymentGateway _stripe = new();

    public void ProcessOrder(decimal amount) =>
        _stripe.ChargeCard("tok_visa", (int)(amount * 100));
}
```

### Good Example

```csharp
// ── Target interface (what our code expects) ──────────────
interface IPaymentProcessor
{
    bool ProcessPayment(decimal amount, string currency);
}

// ── Adaptee: third-party Stripe SDK ──────────────────────
class StripePaymentGateway
{
    public bool ChargeCard(string cardToken, int amountInCents, string currencyCode)
    {
        Console.WriteLine($"  [Stripe SDK] Charged {amountInCents} {currencyCode} on {cardToken}");
        return true;
    }
}

// ── Adaptee: legacy in-house payment system ───────────────
class LegacyPaymentSystem
{
    public void MakePayment(double amount)
    {
        Console.WriteLine($"  [Legacy]     Payment of {amount:F2} processed via old system");
    }
}

// ── Adapter: wraps Stripe and exposes IPaymentProcessor ───
class StripeAdapter(StripePaymentGateway stripe) : IPaymentProcessor
{
    private const string DefaultToken = "tok_visa_test";

    public bool ProcessPayment(decimal amount, string currency)
    {
        int cents = (int)(amount * 100);
        return stripe.ChargeCard(DefaultToken, cents, currency.ToUpper());
    }
}

// ── Adapter: wraps legacy system ─────────────────────────
class LegacyAdapter(LegacyPaymentSystem legacy) : IPaymentProcessor
{
    public bool ProcessPayment(decimal amount, string currency)
    {
        legacy.MakePayment((double)amount);
        return true;
    }
}

// ── Client — depends only on IPaymentProcessor ───────────
class OrderService(IPaymentProcessor processor)
{
    public void ProcessOrder(decimal amount, string currency = "USD")
    {
        var success = processor.ProcessPayment(amount, currency);
        Console.WriteLine($"  Order payment {(success ? "succeeded" : "failed")}.");
    }
}

// ── Demo ──────────────────────────────────────────────────
new OrderService(new StripeAdapter(new StripePaymentGateway()))
    .ProcessOrder(49.99m, "USD");

new OrderService(new LegacyAdapter(new LegacyPaymentSystem()))
    .ProcessOrder(49.99m, "GBP");
```

## Key Takeaways

- The client depends on the target interface, never the adaptee.
- No modification to the third-party or legacy class is needed.
- Object adapter (composition, shown above) is preferred over class adapter (inheritance) in C#.
- Multiple adapters can wrap the same adaptee for different target interfaces.

## When to Use

- You want to use an existing class but its interface doesn't match what you need.
- You are integrating third-party libraries without modifying them.
- You need to make legacy code work with new code through a clean interface boundary.

## When NOT to Use

- You own both sides of the interface — just change the interface to match.
- The adaptation logic becomes so complex it obscures the real business logic; consider a facade or anti-corruption layer.
