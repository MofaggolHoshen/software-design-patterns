# ♟️ Strategy Pattern

The Strategy pattern defines a **family of algorithms**, encapsulates each one, and makes them interchangeable. It lets the algorithm vary independently from clients that use it, replacing conditionals with polymorphism.

## Intent

> Define a family of algorithms, encapsulate each one, and make them interchangeable. Strategy lets the algorithm vary independently from clients that use it.

## Problem

When an operation can be performed in several ways (sorting, pricing, discounting, compression), putting all variants inside one class via `if/switch` violates OCP and SRP. Adding a new algorithm requires editing the class holding the logic.

### Bad Example

```csharp
class ShoppingCart
{
    private readonly string _discountType;

    public ShoppingCart(string discountType) => _discountType = discountType;

    public decimal CalculateTotal(decimal subtotal) => _discountType switch
    {
        "Percentage" => subtotal * 0.90m,   // 10% off
        "Fixed"      => subtotal - 20m,
        "NoDiscount" => subtotal,
        // Adding "LoyaltyPoints" requires editing this switch
        _ => throw new ArgumentException(_discountType)
    };
}
```

### Good Example

```csharp
// ── Strategy interface ────────────────────────────────────
interface IDiscountStrategy
{
    decimal Apply(decimal subtotal);
    string  Description { get; }
}

// ── Concrete Strategies ───────────────────────────────────
class NoDiscountStrategy : IDiscountStrategy
{
    public string Description => "No discount";
    public decimal Apply(decimal subtotal) => subtotal;
}

class PercentageDiscountStrategy(decimal percent) : IDiscountStrategy
{
    public string Description => $"{percent}% off";
    public decimal Apply(decimal subtotal) => subtotal * (1 - percent / 100);
}

class FixedDiscountStrategy(decimal amount) : IDiscountStrategy
{
    public string Description => $"${amount} off";
    public decimal Apply(decimal subtotal) => Math.Max(0, subtotal - amount);
}

class BuyOneGetOneFreeStrategy : IDiscountStrategy
{
    public string Description => "Buy one get one free";
    public decimal Apply(decimal subtotal) => subtotal / 2;
}

// ── Context ───────────────────────────────────────────────
class ShoppingCart(IDiscountStrategy discountStrategy)
{
    public decimal CalculateTotal(decimal subtotal)
    {
        var total = discountStrategy.Apply(subtotal);
        Console.WriteLine($"  Subtotal: {subtotal:C}  |  {discountStrategy.Description}  →  {total:C}");
        return total;
    }

    // Strategy can be swapped at runtime
    public ShoppingCart WithStrategy(IDiscountStrategy strategy) =>
        new(strategy);
}

// ── Demo ──────────────────────────────────────────────────
decimal subtotal = 120m;

var strategies = new IDiscountStrategy[]
{
    new NoDiscountStrategy(),
    new PercentageDiscountStrategy(10),
    new FixedDiscountStrategy(20),
    new BuyOneGetOneFreeStrategy()
};

foreach (var strategy in strategies)
    new ShoppingCart(strategy).CalculateTotal(subtotal);

// Swap strategy at runtime
var cart = new ShoppingCart(new NoDiscountStrategy());
cart.CalculateTotal(subtotal);

var vipCart = cart.WithStrategy(new PercentageDiscountStrategy(25));
vipCart.CalculateTotal(subtotal);
```

## Key Takeaways

- Eliminates `if/switch` chains for selecting algorithms — replaced with polymorphism.
- Each strategy is independently testable and reusable.
- The context does not need to know which strategy is used — it only calls `Apply()`.
- Strategies can be stored in a dictionary keyed by name for runtime selection (e.g., from config).

## When to Use

- Multiple classes differ in only their behaviour — extract the differing algorithm.
- You need runtime-swappable algorithms (sort order, pricing rules, compression codec).
- You want to eliminate conditionals that select among algorithm variants.

## When NOT to Use

- You only have two variants and they will never change — a boolean flag or simple `if` is clearer.
- The strategy needs access to many private details of the context — consider Template Method instead.
