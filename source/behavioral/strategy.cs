// ============================================================
// Strategy Pattern — C# Example
// ============================================================
//
// Intent: Define a family of interchangeable algorithms and select
// one at runtime — replacing if/switch with polymorphism.
//
// Key roles:
//   IDiscountStrategy      — Strategy interface
//   NoDiscountStrategy     — Concrete Strategy
//   PercentageDiscount     — Concrete Strategy
//   FixedDiscount          — Concrete Strategy
//   BuyOneGetOneFree       — Concrete Strategy
//   ShoppingCart           — Context
// ============================================================

// ── Strategy interface ───────────────────────────────────
interface IDiscountStrategy
{
    string Description { get; }
    decimal Apply(decimal subtotal);
}

// ── Concrete Strategies ──────────────────────────────────
class NoDiscountStrategy : IDiscountStrategy
{
    public string Description => "No discount";
    public decimal Apply(decimal subtotal) => subtotal;
}

class PercentageDiscountStrategy(decimal percent) : IDiscountStrategy
{
    public string Description => $"{percent}% off";
    public decimal Apply(decimal subtotal) => Math.Round(subtotal * (1 - percent / 100), 2);
}

class FixedDiscountStrategy(decimal amount) : IDiscountStrategy
{
    public string Description => $"${amount} off";
    public decimal Apply(decimal subtotal) => Math.Max(0, subtotal - amount);
}

class BuyOneGetOneFreeStrategy : IDiscountStrategy
{
    public string Description => "Buy one get one free (50% off)";
    public decimal Apply(decimal subtotal) => Math.Round(subtotal / 2, 2);
}

class LoyaltyDiscountStrategy(int points) : IDiscountStrategy
{
    // 100 points = $1 off, max 20% of subtotal
    private decimal Discount(decimal subtotal) =>
        Math.Min(points / 100m, subtotal * 0.20m);

    public string Description => $"Loyalty ({points} pts)";
    public decimal Apply(decimal subtotal) => Math.Round(subtotal - Discount(subtotal), 2);
}

// ── Context ──────────────────────────────────────────────
class ShoppingCart
{
    private IDiscountStrategy _strategy;

    public ShoppingCart(IDiscountStrategy strategy) => _strategy = strategy;

    // Swap strategy at runtime (e.g., voucher applied at checkout)
    public void ApplyPromotion(IDiscountStrategy strategy) => _strategy = strategy;

    public decimal Checkout(decimal subtotal)
    {
        var total = _strategy.Apply(subtotal);
        Console.WriteLine($"  {_strategy.Description,-35} {subtotal:C} → {total:C}");
        return total;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Strategy Pattern ===\n");

const decimal subtotal = 120m;
Console.WriteLine($"Subtotal: {subtotal:C}\n");

var strategies = new IDiscountStrategy[]
{
    new NoDiscountStrategy(),
    new PercentageDiscountStrategy(10),
    new FixedDiscountStrategy(20),
    new BuyOneGetOneFreeStrategy(),
    new LoyaltyDiscountStrategy(500),
};

foreach (var s in strategies)
    new ShoppingCart(s).Checkout(subtotal);

Console.WriteLine("\n--- Runtime strategy swap ---");
var cart = new ShoppingCart(new NoDiscountStrategy());
cart.Checkout(subtotal);  // No discount

cart.ApplyPromotion(new PercentageDiscountStrategy(25));
cart.Checkout(subtotal);  // VIP 25% off applied at runtime
