// ============================================================
// Builder Pattern — C# Example
// ============================================================
//
// Intent: Separate the construction of a complex object from
// its representation so the same process can produce different
// representations.
//
// Key roles:
//   Email         — Product (immutable after Build())
//   Email.Builder — Concrete Builder with fluent API
// ============================================================

// ── Product ────────────────────────────────────────────────
class Email
{
    public string To { get; private set; } = "";
    public string From { get; private set; } = "";
    public string Subject { get; private set; } = "(no subject)";
    public string? HtmlBody { get; private set; }
    public string? TextBody { get; private set; }
    public bool IsHighPriority { get; private set; }
    public string? AttachmentPath { get; private set; }

    // Private constructor — only the builder creates instances
    private Email() { }

    // ── Fluent Builder ────────────────────────────────────
    public class Builder
    {
        private readonly Email _email;

        // Required parameters go in the constructor
        public Builder(string to, string from)
        {
            _email = new Email { To = to, From = from };
        }

        public Builder WithSubject(string subject)
        {
            _email.Subject = subject;
            return this;
        }

        public Builder WithHtmlBody(string html)
        {
            _email.HtmlBody = html;
            return this;
        }

        public Builder WithTextBody(string text)
        {
            _email.TextBody = text;
            return this;
        }

        public Builder AsHighPriority()
        {
            _email.IsHighPriority = true;
            return this;
        }

        public Builder WithAttachment(string path)
        {
            _email.AttachmentPath = path;
            return this;
        }

        // Validate and return the finished product
        public Email Build()
        {
            if (string.IsNullOrWhiteSpace(_email.To))
                throw new InvalidOperationException("'To' address is required.");
            if (_email.HtmlBody is null && _email.TextBody is null)
                throw new InvalidOperationException("Email must have at least one body.");
            return _email;
        }
    }

    public override string ToString() =>
        $"To={To}, Subject={Subject}, Priority={IsHighPriority}, Attachment={AttachmentPath ?? "none"}";
}

// ── Demo ───────────────────────────────────────────────────
Console.WriteLine("=== Builder Pattern ===\n");

// Minimal email — only required fields + one body
var simple = new Email.Builder("alice@example.com", "noreply@example.com")
    .WithSubject("Welcome!")
    .WithTextBody("Hi Alice, welcome aboard.")
    .Build();
Console.WriteLine($"Simple:   {simple}");

// Rich email with all options
var rich = new Email.Builder("bob@example.com", "sales@example.com")
    .WithSubject("Your invoice is ready")
    .WithHtmlBody("<h1>Invoice</h1><p>See attached.</p>")
    .WithTextBody("Invoice attached.")
    .AsHighPriority()
    .WithAttachment("/invoices/inv-2026-042.pdf")
    .Build();
Console.WriteLine($"Rich:     {rich}");
