# 🧬 Prototype Pattern

The Prototype pattern creates new objects by **copying (cloning) an existing object** rather than constructing one from scratch. This is useful when object creation is expensive or complex, and a pre-configured instance already exists that can be duplicated.

## Intent

> Specify the kinds of objects to create using a prototypical instance, and create new objects by copying this prototype.

## Problem

When object construction is costly (e.g., loading data from a database or setting many configuration properties) and you need many similar objects, creating each one from scratch is wasteful. Duplicating the construction logic in multiple places also violates DRY and introduces maintenance risk.

### Bad Example

```csharp
class ConfigProfile
{
    public string Host        { get; set; } = "";
    public int    Port        { get; set; }
    public int    MaxRetries  { get; set; }
    public bool   UseTls      { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

// Manually copying all fields everywhere — easy to forget a property
var baseProfile = new ConfigProfile
{
    Host = "api.example.com", Port = 443,
    MaxRetries = 3, UseTls = true
};
baseProfile.Headers["Authorization"] = "Bearer token123";

// "Clone" by manually setting every field — fragile, breaks if new fields are added
var profileForRegion2 = new ConfigProfile
{
    Host       = "api-eu.example.com", // only this differs
    Port       = baseProfile.Port,
    MaxRetries = baseProfile.MaxRetries,
    UseTls     = baseProfile.UseTls,
    Headers    = new Dictionary<string, string>(baseProfile.Headers)
    // New field added to ConfigProfile? This copy is silently wrong.
};
```

### Good Example

```csharp
// ── Prototype interface ────────────────────────────────────
interface IPrototype<T>
{
    T DeepCopy();
}

// ── Concrete prototype ────────────────────────────────────
class ConfigProfile : IPrototype<ConfigProfile>
{
    public string  Host       { get; set; } = "";
    public int     Port       { get; set; }
    public int     MaxRetries { get; set; }
    public bool    UseTls     { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();

    // All copying logic lives in one place — add a field here, cloning is automatically correct
    public ConfigProfile DeepCopy() => new()
    {
        Host       = Host,
        Port       = Port,
        MaxRetries = MaxRetries,
        UseTls     = UseTls,
        Headers    = new Dictionary<string, string>(Headers)
    };
}

// ── Usage ─────────────────────────────────────────────────
var baseProfile = new ConfigProfile
{
    Host = "api.example.com", Port = 443,
    MaxRetries = 3, UseTls = true
};
baseProfile.Headers["Authorization"] = "Bearer token123";

// Clone and adjust only what differs
var euProfile = baseProfile.DeepCopy();
euProfile.Host = "api-eu.example.com";

Console.WriteLine(baseProfile.Host); // api.example.com
Console.WriteLine(euProfile.Host);   // api-eu.example.com
Console.WriteLine(euProfile.Headers["Authorization"]); // Bearer token123 — copied correctly
```

## Key Takeaways

- All copy logic is centralised in `DeepCopy()` — adding a new field means updating one place.
- Separates what is shared from what differs between instances.
- Shallow vs. deep copy matters: reference types (like `Dictionary`, `List`) must be explicitly deep-copied to avoid shared-state bugs.
- C# records support non-destructive mutation with `with` expressions, which is a built-in prototype pattern.

## When to Use

- Object creation is expensive (database round-trip, complex initialisation) and a template instance already exists.
- You need many objects that differ only slightly from a common base configuration.
- You want to avoid subclassing just to vary initial state.

## When NOT to Use

- Objects are simple value types or records — use `with` expressions or a constructor directly.
- Objects contain circular references, making deep copy complex and error-prone.
