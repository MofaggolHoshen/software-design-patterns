# 🔀 State Pattern

The State pattern allows an object to **alter its behaviour when its internal state changes**. The object will appear to change its class. State-specific logic is moved out of a bloated `switch` and into dedicated state classes.

## Intent

> Allow an object to alter its behaviour when its internal state changes. The object will appear to change its class.

## Problem

When an object has many states and behaviour differs per state, a `switch` statement on a state enum spreads across the class. Each new state requires modifying every `switch` block. The class violates the Open/Closed Principle and becomes hard to test in isolation.

### Bad Example

```csharp
enum OrderState { Pending, Processing, Shipped, Delivered, Cancelled }

class OrderBad
{
    public OrderState State { get; private set; } = OrderState.Pending;

    public void Next()
    {
        switch (State)
        {
            case OrderState.Pending:    State = OrderState.Processing; break;
            case OrderState.Processing: State = OrderState.Shipped;    break;
            case OrderState.Shipped:    State = OrderState.Delivered;  break;
            default: throw new InvalidOperationException("Cannot advance.");
        }
        // Adding a new state requires editing every switch in the class
    }

    public void Cancel()
    {
        if (State == OrderState.Delivered || State == OrderState.Cancelled)
            throw new InvalidOperationException("Cannot cancel.");
        State = OrderState.Cancelled;
    }
}
```

### Good Example

```csharp
// ── State interface ────────────────────────────────────────
interface IOrderState
{
    void Next(Order order);
    void Cancel(Order order);
    string Name { get; }
}

// ── Context ───────────────────────────────────────────────
class Order
{
    public IOrderState CurrentState { get; private set; } = new PendingState();

    public void SetState(IOrderState state)
    {
        Console.WriteLine($"  State: {CurrentState.Name} → {state.Name}");
        CurrentState = state;
    }

    public void Next()   => CurrentState.Next(this);
    public void Cancel() => CurrentState.Cancel(this);
}

// ── Concrete States ───────────────────────────────────────
class PendingState : IOrderState
{
    public string Name => "Pending";
    public void Next(Order o)   => o.SetState(new ProcessingState());
    public void Cancel(Order o) => o.SetState(new CancelledState());
}

class ProcessingState : IOrderState
{
    public string Name => "Processing";
    public void Next(Order o)   => o.SetState(new ShippedState());
    public void Cancel(Order o) => o.SetState(new CancelledState());
}

class ShippedState : IOrderState
{
    public string Name => "Shipped";
    public void Next(Order o)   => o.SetState(new DeliveredState());
    public void Cancel(Order o) => Console.WriteLine("  Cannot cancel a shipped order.");
}

class DeliveredState : IOrderState
{
    public string Name => "Delivered";
    public void Next(Order o)   => Console.WriteLine("  Order already delivered.");
    public void Cancel(Order o) => Console.WriteLine("  Cannot cancel a delivered order.");
}

class CancelledState : IOrderState
{
    public string Name => "Cancelled";
    public void Next(Order o)   => Console.WriteLine("  Cannot advance a cancelled order.");
    public void Cancel(Order o) => Console.WriteLine("  Already cancelled.");
}

// ── Demo ──────────────────────────────────────────────────
var order = new Order();
order.Next();    // Pending → Processing
order.Next();    // Processing → Shipped
order.Cancel();  // Cannot cancel a shipped order
order.Next();    // Shipped → Delivered
order.Cancel();  // Cannot cancel a delivered order
```

## Key Takeaways

- All state-specific logic lives in the state class — the context stays lean.
- Adding a new state = add a new class; no existing code changes.
- States can hold their own data (e.g., timestamps, retry counts).
- Transitions can be driven by the context or by the state itself, depending on who owns the transition logic.

## When to Use

- An object behaves differently depending on its state, and the number of states is significant.
- You want to avoid large `switch`/`if` chains that must be updated when adding states.
- State-related behaviour must be tested independently.

## When NOT to Use

- There are only 2 states and the logic is trivial — a boolean flag is simpler.
- State transitions are so few that a small `switch` is more readable than a hierarchy of classes.
