// ============================================================
// Chain of Responsibility Pattern — C# Example
// ============================================================
//
// Intent: Pass a request along a chain of handlers until one handles it.
//
// Key roles:
//   SupportHandler — Abstract handler with SetNext/Handle
//   Level1Handler, Level2Handler, ManagementHandler — Concrete handlers
// ============================================================

enum SupportLevel { Basic, Technical, Management, Security }

record SupportTicket(string Title, SupportLevel Level);

// ── Abstract Handler ────────────────────────────────────
abstract class SupportHandler
{
    private SupportHandler? _next;

    // Returns next handler for fluent chaining: l1.SetNext(l2).SetNext(l3)
    public SupportHandler SetNext(SupportHandler next)
    {
        _next = next;
        return next;
    }

    public void Handle(SupportTicket ticket)
    {
        if (CanHandle(ticket))
            Process(ticket);
        else if (_next is not null)
            _next.Handle(ticket);
        else
            Console.WriteLine($"  [Unhandled] No handler for: {ticket.Title}");
    }

    protected abstract bool CanHandle(SupportTicket ticket);
    protected abstract void Process(SupportTicket ticket);
}

// ── Concrete Handlers ────────────────────────────────────
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

// ── Demo ─────────────────────────────────────────────────
Console.WriteLine("=== Chain of Responsibility Pattern ===\n");

var l1 = new Level1Handler();
var l2 = new Level2Handler();
var mgmt = new ManagementHandler();

// Build the chain: L1 → L2 → Management
l1.SetNext(l2).SetNext(mgmt);

var tickets = new[]
{
    new SupportTicket("Password reset",   SupportLevel.Basic),
    new SupportTicket("DB performance",   SupportLevel.Technical),
    new SupportTicket("Contract dispute", SupportLevel.Management),
    new SupportTicket("Zero-day breach",  SupportLevel.Security),   // no handler
};

foreach (var ticket in tickets)
{
    Console.WriteLine($"Ticket: {ticket.Title} [{ticket.Level}]");
    l1.Handle(ticket);
    Console.WriteLine();
}
