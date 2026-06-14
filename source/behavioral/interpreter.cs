// ============================================================
// Interpreter Pattern — C# Example
// ============================================================
//
// Intent: Define a grammar and an interpreter for a simple language.
//
// Grammar: boolean expressions using AND, OR, NOT over named facts.
//   Expression ::= Fact | NOT Expression | Expression AND Expression
//                        | Expression OR Expression
//
// Key roles:
//   IExpression  — Abstract Expression
//   FactExpression  — Terminal: looks up a named fact
//   AndExpression   — Non-terminal: logical AND
//   OrExpression    — Non-terminal: logical OR
//   NotExpression   — Non-terminal: logical NOT
// ============================================================

// ── Abstract Expression ──────────────────────────────────
interface IExpression
{
    bool Interpret(Dictionary<string, bool> context);
    string Display();
}

// ── Terminal Expression — single named fact ──────────────
class FactExpression(string name) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        ctx.TryGetValue(name, out var v) && v;
    public string Display() => name;
}

// ── Non-Terminal: AND ────────────────────────────────────
class AndExpression(IExpression left, IExpression right) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        left.Interpret(ctx) && right.Interpret(ctx);
    public string Display() => $"({left.Display()} AND {right.Display()})";
}

// ── Non-Terminal: OR ─────────────────────────────────────
class OrExpression(IExpression left, IExpression right) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        left.Interpret(ctx) || right.Interpret(ctx);
    public string Display() => $"({left.Display()} OR {right.Display()})";
}

// ── Non-Terminal: NOT ────────────────────────────────────
class NotExpression(IExpression expr) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) => !expr.Interpret(ctx);
    public string Display() => $"NOT {expr.Display()}";
}

// ── Build access-control rule ────────────────────────────
// Rule: isAdmin AND (emailVerified OR phoneVerified) AND NOT isSuspended
IExpression rule =
    new AndExpression(
        new FactExpression("isAdmin"),
        new AndExpression(
            new OrExpression(
                new FactExpression("emailVerified"),
                new FactExpression("phoneVerified")),
            new NotExpression(
                new FactExpression("isSuspended"))));

Console.WriteLine("=== Interpreter Pattern ===\n");
Console.WriteLine($"Rule: {rule.Display()}\n");

// ── Evaluate for different users ─────────────────────────
var users = new[]
{
    (Name: "Alice", Facts: new Dictionary<string, bool>
    {
        ["isAdmin"]       = true,
        ["emailVerified"] = true,
        ["phoneVerified"] = false,
        ["isSuspended"]   = false
    }),
    (Name: "Bob", Facts: new Dictionary<string, bool>
    {
        ["isAdmin"]       = true,
        ["emailVerified"] = false,
        ["phoneVerified"] = false,
        ["isSuspended"]   = false
    }),
    (Name: "Carol", Facts: new Dictionary<string, bool>
    {
        ["isAdmin"]       = true,
        ["emailVerified"] = true,
        ["phoneVerified"] = true,
        ["isSuspended"]   = true    // suspended!
    }),
};

foreach (var (name, facts) in users)
    Console.WriteLine($"  {name,-6} → {(rule.Interpret(facts) ? "ACCESS GRANTED" : "ACCESS DENIED")}");
