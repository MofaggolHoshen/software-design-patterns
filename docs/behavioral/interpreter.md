# 📖 Interpreter Pattern

The Interpreter pattern defines a **grammar** for a simple language and provides an **interpreter** that processes sentences in that language. Each grammar rule maps to a class, and complex expressions are built by composing simpler ones.

## Intent

> Given a language, define a representation for its grammar along with an interpreter that uses the representation to interpret sentences in the language.

## Problem

When an application must evaluate expressions, queries, or commands provided as strings (e.g., filter rules, simple scripting, DSLs), parsing them with ad-hoc `if/switch` chains becomes unmanageable as the grammar grows.

### Bad Example

```csharp
// Ad-hoc parser for "AND"/"OR" filters — breaks immediately with nested expressions
class FilterEvaluator
{
    public bool Evaluate(string expression, Dictionary<string, bool> facts)
    {
        if (expression.Contains(" AND "))
        {
            var parts = expression.Split(" AND ");
            return facts[parts[0].Trim()] && facts[parts[1].Trim()];
        }
        if (expression.Contains(" OR "))
        {
            var parts = expression.Split(" OR ");
            return facts[parts[0].Trim()] || facts[parts[1].Trim()];
        }
        return facts[expression.Trim()];
        // Nested expressions like "(A AND B) OR C" are impossible here
    }
}
```

### Good Example

```csharp
// ── Abstract Expression ────────────────────────────────────
interface IExpression
{
    bool Interpret(Dictionary<string, bool> context);
}

// ── Terminal Expression — a single fact ───────────────────
class FactExpression(string factName) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        ctx.TryGetValue(factName, out var value) && value;
}

// ── Non-Terminal: AND ────────────────────────────────────
class AndExpression(IExpression left, IExpression right) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        left.Interpret(ctx) && right.Interpret(ctx);
}

// ── Non-Terminal: OR ─────────────────────────────────────
class OrExpression(IExpression left, IExpression right) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) =>
        left.Interpret(ctx) || right.Interpret(ctx);
}

// ── Non-Terminal: NOT ────────────────────────────────────
class NotExpression(IExpression expr) : IExpression
{
    public bool Interpret(Dictionary<string, bool> ctx) => !expr.Interpret(ctx);
}

// ── Build & evaluate expressions ──────────────────────────
// Rule: user is admin AND (email verified OR phone verified) AND NOT suspended
IExpression rule =
    new AndExpression(
        new FactExpression("isAdmin"),
        new AndExpression(
            new OrExpression(
                new FactExpression("emailVerified"),
                new FactExpression("phoneVerified")),
            new NotExpression(
                new FactExpression("isSuspended"))));

var user1 = new Dictionary<string, bool>
{
    ["isAdmin"]       = true,
    ["emailVerified"] = true,
    ["phoneVerified"] = false,
    ["isSuspended"]   = false
};

var user2 = new Dictionary<string, bool>
{
    ["isAdmin"]       = true,
    ["emailVerified"] = false,
    ["phoneVerified"] = false,
    ["isSuspended"]   = false
};

Console.WriteLine($"User1 passes rule: {rule.Interpret(user1)}");  // True
Console.WriteLine($"User2 passes rule: {rule.Interpret(user2)}");  // False
```

## Key Takeaways

- Each grammar rule becomes a class — easy to add new rules without modifying existing ones.
- Complex expressions are built by composing simple expression objects (Composite structure).
- The context (`Dictionary`) holds the current state evaluated by terminal expressions.
- For large grammars, prefer a parser generator (ANTLR, Irony) over hand-coded interpreters.

## When to Use

- You need to interpret sentences in a simple, well-defined grammar.
- Building rule-engines, expression evaluators, or configuration DSLs.
- The grammar is stable and has a small number of rules.

## When NOT to Use

- The grammar is complex or changes frequently — use a lexer/parser generator instead.
- Performance is critical — building and traversing expression trees has overhead.
