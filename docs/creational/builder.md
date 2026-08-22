# 🔨 Builder Pattern

The Builder pattern separates the **construction** of a complex object from its **representation**, allowing the same construction process to create different representations. Instead of a constructor with a long parameter list, the Builder provides a fluent API that assembles the object step-by-step.

## Intent

> Separate the construction of a complex object from its representation so that the same construction process can create different representations.

## Problem

When an object requires many optional parameters or a multi-step initialisation sequence, telescoping constructors become unreadable and error-prone. Passing `null` or default values for unused parameters is a code smell, and the order of arguments is easy to confuse.

### Bad Example

```csharp
// 8-parameter constructor — which bool is which?
var email = new Email(
    "alice@example.com",  // to
    "bob@example.com",    // from
    "Hello",              // subject
    "<h1>Hi</h1>",        // htmlBody
    null,                 // textBody
    true,                 // isHighPriority
    false,                // requestReadReceipt
    null                  // attachmentPath
);
// Easy to swap args; hard to read at the call site
```

### Good Example

```csharp
class Email
{
    public string To              { get; private set; } = "";
    public string From            { get; private set; } = "";
    public string Subject         { get; private set; } = "";
    public string? HtmlBody       { get; private set; }
    public string? TextBody       { get; private set; }
    public bool   IsHighPriority  { get; private set; }
    public string? AttachmentPath { get; private set; }

    private Email() { }

    // ── Fluent Builder ────────────────────────────────────
    public class Builder(string to, string from)
    {
        private readonly Email _email = new() { To = to, From = from };

        public Builder Subject(string subject)
            { _email.Subject = subject; return this; }

        public Builder HtmlBody(string html)
            { _email.HtmlBody = html; return this; }

        public Builder TextBody(string text)
            { _email.TextBody = text; return this; }

        public Builder HighPriority()
            { _email.IsHighPriority = true; return this; }

        public Builder Attachment(string path)
            { _email.AttachmentPath = path; return this; }

        public Email Build() => _email;
    }
}

// ── Usage ─────────────────────────────────────────────────
var email = new Email.Builder("alice@example.com", "bob@example.com")
    .Subject("Hello")
    .HtmlBody("<h1>Hi</h1>")
    .HighPriority()
    .Build();

Console.WriteLine($"To: {email.To}, Priority: {email.IsHighPriority}");
// To: alice@example.com, Priority: True
```

## Key Takeaways

- Replaces telescoping constructors with a readable, self-documenting fluent API.
- Only call the methods that matter — unused properties stay at sensible defaults.
- The `Build()` method is the natural place to validate the final object before returning it.
- The built object can be made immutable after `Build()` is called.

## When to Use

- An object has many optional parameters or configuration options.
- Construction involves multiple steps that must occur in a specific order.
- You want a readable, self-documenting object-creation API.

## When NOT to Use

- The object is simple with only 1–2 required fields — a constructor is fine.
- You need to create many instances in a tight loop where the builder allocation is a concern (consider object pooling instead).
