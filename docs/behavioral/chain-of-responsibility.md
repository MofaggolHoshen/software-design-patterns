# ⛓️ Chain of Responsibility Pattern

The Chain of Responsibility pattern passes a request along a **chain of handlers**. Each handler decides either to process the request or pass it to the next handler in the chain. This decouples the sender from specific receivers and allows the chain to be composed dynamically.

## Intent

> Avoid coupling the sender of a request to its receiver by giving more than one object a chance to handle the request. Chain the receiving objects and pass the request along the chain until one handles it.

## Problem

Without this pattern, the request dispatcher contains a growing `if/else` or `switch` that couples it to every possible handler. Adding a new handler requires modifying the dispatcher, and the order of evaluation is hard to change at runtime.

### Bad Example

```csharp
// Dispatcher is tightly coupled to all handler types
class SupportDesk
{
    public void Handle(SupportTicket ticket)
    {
        if (ticket.Level == SupportLevel.Basic)
            Console.WriteLine("Level 1 support handles it.");
        else if (ticket.Level == SupportLevel.Technical)
            Console.WriteLine("Level 2 technical support handles it.");
        else if (ticket.Level == SupportLevel.Management)
            Console.WriteLine("Management handles it.");
        else
            Console.WriteLine("Unhandled ticket.");
        // Adding Level 3 requires editing this method
    }
}
```

### Good Example

```csharp
enum SupportLevel { Basic, Technical, Management, Security }

record SupportTicket(string Title, SupportLevel Level);

// ── Abstract handler ───────────────────────────────────────
abstract class SupportHandler
{
    private SupportHandler? _next;

    public SupportHandler SetNext(SupportHandler next)
    {
        _next = next;
        return next; // fluent chaining
    }

    public void Handle(SupportTicket ticket)
    {
        if (CanHandle(ticket))
            Process(ticket);
        else if (_next is not null)
            _next.Handle(ticket);
        else
            Console.WriteLine($"  No handler for: {ticket.Title}");
    }

    protected abstract bool CanHandle(SupportTicket ticket);
    protected abstract void Process(SupportTicket ticket);
}

// ── Concrete handlers ──────────────────────────────────────
class Level1Handler : SupportHandler
{
    protected override bool CanHandle(SupportTicket t) => t.Level == SupportLevel.Basic;
    protected override void Process(SupportTicket t) =>
        Console.WriteLine($"  [L1] Resolved '{t.Title}' with FAQ article.");
}

class Level2Handler : SupportHandler
{
    protected override bool CanHandle(SupportTicket t) => t.Level == SupportLevel.Technical;
    protected override void Process(SupportTicket t) =>
        Console.WriteLine($"  [L2] Resolved '{t.Title}' by reviewing logs.");
}

class ManagementHandler : SupportHandler
{
    protected override bool CanHandle(SupportTicket t) => t.Level == SupportLevel.Management;
    protected override void Process(SupportTicket t) =>
        Console.WriteLine($"  [Mgmt] Resolved '{t.Title}' via escalation call.");
}

// ── Compose the chain ─────────────────────────────────────
var l1   = new Level1Handler();
var l2   = new Level2Handler();
var mgmt = new ManagementHandler();

l1.SetNext(l2).SetNext(mgmt);   // L1 → L2 → Management

l1.Handle(new SupportTicket("Password reset",   SupportLevel.Basic));
l1.Handle(new SupportTicket("DB performance",   SupportLevel.Technical));
l1.Handle(new SupportTicket("Contract dispute", SupportLevel.Management));
l1.Handle(new SupportTicket("Breach alert",     SupportLevel.Security));  // no handler
```

## Key Takeaways

- Senders and handlers are decoupled — the sender only knows the first handler in the chain.
- Handlers can be added, removed, or reordered at runtime without touching the sender.
- Not every request must be handled; the chain can intentionally pass through all handlers.
- Use with middleware pipelines (ASP.NET Core middleware is a real-world example of this pattern).

## When to Use

- More than one object may handle a request, and the handler is not known a priori.
- You want to issue a request without specifying the receiver explicitly.
- The set of handlers should be configurable dynamically.

## When NOT to Use

- Exactly one handler always processes the request — a simple strategy or `if/switch` is clearer.
- You need a guaranteed response; a chain can silently drop requests if no handler matches.
