// ============================================================
// Observer Pattern — C# Example
// ============================================================
//
// Intent: Define a one-to-many dependency so that when one object
// changes state, all dependents are notified automatically.
//
// Two approaches shown:
//   1. Custom IStockObserver interface
//   2. C# event/delegate (built-in Observer)
// ============================================================

// ── Observer interface ───────────────────────────────────
interface IStockObserver
{
    void OnPriceChanged(string symbol, decimal newPrice);
}

// ── Subject ──────────────────────────────────────────────
class StockTicker
{
    private readonly Dictionary<string, decimal> _prices = new();
    private readonly List<IStockObserver> _observers = new();

    public void Subscribe(IStockObserver observer) => _observers.Add(observer);
    public void Unsubscribe(IStockObserver observer) => _observers.Remove(observer);

    public void UpdatePrice(string symbol, decimal price)
    {
        _prices[symbol] = price;
        Console.WriteLine($"  [{symbol}] price updated to {price:C}");
        foreach (var obs in _observers)
            obs.OnPriceChanged(symbol, price);
    }
}

// ── Concrete Observers ───────────────────────────────────
class EmailAlertObserver(decimal threshold) : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price)
    {
        if (price > threshold)
            Console.WriteLine($"    [Email]     ALERT {symbol} exceeded {threshold:C}: now {price:C}");
    }
}

class DashboardObserver : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price) =>
        Console.WriteLine($"    [Dashboard] {symbol} widget updated to {price:C}");
}

class AuditLogObserver : IStockObserver
{
    public void OnPriceChanged(string symbol, decimal price) =>
        Console.WriteLine($"    [Audit]     {DateTime.UtcNow:HH:mm:ss.fff} {symbol} → {price:C}");
}

// ── C# idiomatic: event/delegate ─────────────────────────
class StockTickerEvents
{
    public event Action<string, decimal>? PriceChanged;

    public void UpdatePrice(string symbol, decimal price) =>
        PriceChanged?.Invoke(symbol, price);
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Observer Pattern ===\n");

var ticker = new StockTicker();
var email = new EmailAlertObserver(150m);
var dash = new DashboardObserver();
var audit = new AuditLogObserver();

ticker.Subscribe(email);
ticker.Subscribe(dash);
ticker.Subscribe(audit);

ticker.UpdatePrice("AAPL", 148.50m);
Console.WriteLine();
ticker.UpdatePrice("AAPL", 152.00m);

Console.WriteLine("\n--- Unsubscribe email, new update ---");
ticker.Unsubscribe(email);
ticker.UpdatePrice("AAPL", 155.00m); // email no longer receives this

Console.WriteLine("\n--- C# event/delegate approach ---");
var tickerEvents = new StockTickerEvents();
tickerEvents.PriceChanged += (sym, p) => Console.WriteLine($"  Handler A: {sym} = {p:C}");
tickerEvents.PriceChanged += (sym, p) => Console.WriteLine($"  Handler B: {sym} = {p:C}");
tickerEvents.UpdatePrice("MSFT", 420.00m);
