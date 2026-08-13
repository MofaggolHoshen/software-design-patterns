# 👁️ Observer Pattern

The Observer pattern defines a **one-to-many dependency** between objects so that when one object (the subject) changes state, all its dependents (observers) are notified and updated automatically. It is the backbone of event-driven programming.

## Intent

> Define a one-to-many dependency between objects so that when one object changes state, all its dependents are notified and updated automatically.

## Problem

When multiple parts of a system must react to changes in a single object, hard-coding direct calls to each listener creates tight coupling. Adding or removing a listener requires modifying the subject, and the subject cannot be reused without carrying all its dependents.

### Bad Example

```csharp
class StockPriceBad
{
    private decimal _price;
    // Direct references to every observer — tight coupling
    private readonly EmailAlertService  _email  = new();
    private readonly DashboardWidget    _widget = new();

    public void SetPrice(decimal price)
    {
        _price = price;
        _email.SendAlert($"Price changed to {price}");   // what if email is disabled?
        _widget.Refresh(price);                          // what if widget isn't shown?
        // Adding a third observer requires editing this class
    }
}
```

### Good Example

```csharp
// ── Observer interface ────────────────────────────────────
interface IStockObserver
{
    void OnPriceChanged(string symbol, decimal newPrice);
}

// ── Subject ───────────────────────────────────────────────
class StockTicker
{
    private readonly Dictionary<string, decimal>      _prices    = new();
    private readonly List<IStockObserver>              _observers = new();

    public void Subscribe(IStockObserver observer)   => _observers.Add(observer);
    public void Unsubscribe(IStockObserver observer) => _observers.Remove(observer);

    public void UpdatePrice(string symbol, decimal price)
    {
        _prices[symbol] = price;
        Notify(symbol, price);
    }

    private void Notify(string symbol, decimal price)
    {
        foreach (var obs in _observers)
            obs.OnPriceChanged(symbol, price);
    }
}

// ── Concrete Observers ────────────────────────────────────
class EmailAlertObserver(string threshold) : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price)
    {
        if (price > decimal.Parse(threshold))
            Console.WriteLine($"  [Email] ALERT: {symbol} hit {price:C}");
    }
}

class DashboardObserver : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price) =>
        Console.WriteLine($"  [Dashboard] {symbol} = {price:C}");
}

class AuditLogObserver : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price) =>
        Console.WriteLine($"  [Audit] {DateTime.UtcNow:HH:mm:ss} {symbol} → {price:C}");
}

// ── C# idiomatic: events/delegates (built-in observer) ─────
class StockTickerEvents
{
    public event Action<string, decimal>? PriceChanged;

    public void UpdatePrice(string symbol, decimal price) =>
        PriceChanged?.Invoke(symbol, price);
}

// ── Demo ──────────────────────────────────────────────────
var ticker = new StockTicker();
ticker.Subscribe(new EmailAlertObserver("150"));
ticker.Subscribe(new DashboardObserver());
ticker.Subscribe(new AuditLogObserver());

ticker.UpdatePrice("AAPL", 148.50m);
ticker.UpdatePrice("AAPL", 152.00m);

Console.WriteLine("\nC# event-based observer:");
var tickerEvents = new StockTickerEvents();
tickerEvents.PriceChanged += (sym, p) => Console.WriteLine($"  Handler 1: {sym} = {p:C}");
tickerEvents.PriceChanged += (sym, p) => Console.WriteLine($"  Handler 2: {sym} = {p:C}");
tickerEvents.UpdatePrice("MSFT", 420.00m);
```

## Key Takeaways

- Subject and observers are loosely coupled — the subject only knows the `IStockObserver` interface.
- Observers can be added or removed at runtime without touching the subject.
- C# `.NET` events (`event Action<>`, `EventHandler`) are a built-in implementation of this pattern.
- Be careful with memory leaks: unsubscribe observers when they are no longer needed.

## When to Use

- A change in one object requires updating an unknown number of others.
- Objects should be able to notify other objects without knowing who they are.
- Implementing event-driven systems, reactive UIs, or pub/sub messaging.

## When NOT to Use

- The order of notification matters in a way that is difficult to control.
- Observers hold strong references to the subject and create memory leaks (use weak references or explicit unsubscription).
