// ============================================================
// Adapter Pattern — C# Example
// ============================================================
//
// Intent: Convert one interface into another that clients expect.
//
// Key roles:
//   IPaymentProcessor   — Target interface
//   StripePaymentGateway — Adaptee (third-party)
//   LegacyPaymentSystem  — Adaptee (legacy)
//   StripeAdapter        — Adapter
//   LegacyAdapter        — Adapter
// ============================================================

// ── Target interface ──────────────────────────────────────
interface IPaymentProcessor
{
    bool ProcessPayment(decimal amount, string currency);
}

// ── Adaptee 1: third-party Stripe SDK ────────────────────
class StripePaymentGateway
{
    public bool ChargeCard(string cardToken, int amountInCents, string currencyCode)
    {
        Console.WriteLine($"  [Stripe] Charged {amountInCents} {currencyCode} on {cardToken}");
        return true;
    }
}

// ── Adaptee 2: legacy in-house system ────────────────────
class LegacyPaymentSystem
{
    public void MakePayment(double amount)
    {
        Console.WriteLine($"  [Legacy] Payment of {amount:F2} processed via old system");
    }
}

// ── Adapter 1: wraps Stripe ──────────────────────────────
class StripeAdapter(StripePaymentGateway stripe) : IPaymentProcessor
{
    private const string TestToken = "tok_visa_test";

    public bool ProcessPayment(decimal amount, string currency)
    {
        int cents = (int)(amount * 100);
        return stripe.ChargeCard(TestToken, cents, currency.ToUpper());
    }
}

// ── Adapter 2: wraps legacy system ───────────────────────
class LegacyPaymentAdapter(LegacyPaymentSystem legacy) : IPaymentProcessor
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
    public bool PlaceOrder(string orderId, decimal amount, string currency = "USD")
    {
        Console.WriteLine($"  Processing order {orderId} for {amount:C} {currency}");
        return processor.ProcessPayment(amount, currency);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Adapter Pattern ===\n");

Console.WriteLine("--- Using Stripe adapter ---");
new OrderService(new StripeAdapter(new StripePaymentGateway()))
    .PlaceOrder("ORD-001", 49.99m, "USD");

Console.WriteLine("\n--- Using Legacy adapter ---");
new OrderService(new LegacyPaymentAdapter(new LegacyPaymentSystem()))
    .PlaceOrder("ORD-002", 29.99m, "GBP");

// Swapping payment provider = swap the adapter — OrderService is untouched
