// ============================================================
// State Pattern — C# Example
// ============================================================
//
// Intent: Allow an object to alter its behaviour when its internal
// state changes — replacing switch/if chains with state objects.
//
// Key roles:
//   IOrderState       — State interface
//   Order             — Context
//   PendingState, ProcessingState, ShippedState,
//   DeliveredState, CancelledState — Concrete States
// ============================================================

// ── State interface ──────────────────────────────────────
interface IOrderState
{
    string Name { get; }
    void Next(Order order);
    void Cancel(Order order);
    void PrintInfo();
}

// ── Context ──────────────────────────────────────────────
class Order
{
    public IOrderState State { get; private set; } = new PendingState();
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    public void Transition(IOrderState newState)
    {
        Console.WriteLine($"  [{Id}] {State.Name} → {newState.Name}");
        State = newState;
    }

    public void Next() => State.Next(this);
    public void Cancel() => State.Cancel(this);
}

// ── Concrete States ───────────────────────────────────────
class PendingState : IOrderState
{
    public string Name => "Pending";
    public void Next(Order o) => o.Transition(new ProcessingState());
    public void Cancel(Order o) => o.Transition(new CancelledState());
    public void PrintInfo() => Console.WriteLine("  Order awaiting payment confirmation.");
}

class ProcessingState : IOrderState
{
    public string Name => "Processing";
    public void Next(Order o) => o.Transition(new ShippedState());
    public void Cancel(Order o) => o.Transition(new CancelledState());
    public void PrintInfo() => Console.WriteLine("  Order is being picked and packed.");
}

class ShippedState : IOrderState
{
    public string Name => "Shipped";
    public void Next(Order o) => o.Transition(new DeliveredState());
    public void Cancel(Order o) => Console.WriteLine("  Cannot cancel: item already shipped. Initiate return.");
    public void PrintInfo() => Console.WriteLine("  Order is in transit.");
}

class DeliveredState : IOrderState
{
    public string Name => "Delivered";
    public void Next(Order o) => Console.WriteLine("  Order is already delivered — workflow complete.");
    public void Cancel(Order o) => Console.WriteLine("  Cannot cancel a delivered order. Request a refund.");
    public void PrintInfo() => Console.WriteLine("  Order delivered successfully.");
}

class CancelledState : IOrderState
{
    public string Name => "Cancelled";
    public void Next(Order o) => Console.WriteLine("  Cannot advance a cancelled order.");
    public void Cancel(Order o) => Console.WriteLine("  Order is already cancelled.");
    public void PrintInfo() => Console.WriteLine("  Order was cancelled.");
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== State Pattern ===\n");

var order = new Order();
order.State.PrintInfo();

order.Next();    // Pending → Processing
order.Next();    // Processing → Shipped
order.Cancel();  // Cannot cancel — already shipped
order.Next();    // Shipped → Delivered
order.Cancel();  // Cannot cancel — already delivered
order.Next();    // Nothing to advance

Console.WriteLine("\n--- Order with early cancellation ---");
var order2 = new Order();
order2.Next();    // Pending → Processing
order2.Cancel();  // Processing → Cancelled
order2.Next();    // Cannot advance — cancelled
