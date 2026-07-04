// ============================================================
// Prototype Pattern — C# Example
// ============================================================
//
// Intent: Create new objects by copying (cloning) an existing
// prototypical instance, rather than constructing from scratch.
//
// Key roles:
//   IPrototype<T>  — Prototype interface with DeepCopy
//   ConfigProfile  — Concrete Prototype
//   ServerConfig   — Demonstrates shallow vs. deep copy pitfall
// ============================================================

// ── Prototype interface ────────────────────────────────────
interface IPrototype<T>
{
    T DeepCopy();
}

// ── Concrete Prototype ────────────────────────────────────
class ConfigProfile : IPrototype<ConfigProfile>
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public int MaxRetries { get; set; }
    public bool UseTls { get; set; }
    public List<string> AllowedIPs { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();

    // All copy logic lives here — add a field once and cloning stays correct
    public ConfigProfile DeepCopy() => new ConfigProfile
    {
        Host = Host,
        Port = Port,
        MaxRetries = MaxRetries,
        UseTls = UseTls,
        AllowedIPs = new List<string>(AllowedIPs),              // deep copy reference type
        Headers = new Dictionary<string, string>(Headers)    // deep copy reference type
    };

    public override string ToString() =>
        $"Host={Host}:{Port}, TLS={UseTls}, Retries={MaxRetries}, " +
        $"IPs=[{string.Join(",", AllowedIPs)}], Headers={Headers.Count}";
}

// ── Demo ───────────────────────────────────────────────────
Console.WriteLine("=== Prototype Pattern ===\n");

// Base prototype — expensive to set up, so we configure it once
var baseProfile = new ConfigProfile
{
    Host = "api.example.com",
    Port = 443,
    MaxRetries = 3,
    UseTls = true
};
baseProfile.AllowedIPs.Add("10.0.0.1");
baseProfile.AllowedIPs.Add("10.0.0.2");
baseProfile.Headers["Authorization"] = "Bearer global-token";
baseProfile.Headers["Content-Type"] = "application/json";

Console.WriteLine($"Base:    {baseProfile}");

// Clone 1 — EU region, different host only
var euProfile = baseProfile.DeepCopy();
euProfile.Host = "api-eu.example.com";
euProfile.AllowedIPs.Add("10.1.0.1"); // adding to clone does NOT affect base

Console.WriteLine($"EU:      {euProfile}");

// Clone 2 — staging environment
var stagingProfile = baseProfile.DeepCopy();
stagingProfile.Host = "api-staging.example.com";
stagingProfile.UseTls = false;
stagingProfile.MaxRetries = 5;

Console.WriteLine($"Staging: {stagingProfile}");

// Prove independence — base AllowedIPs unchanged
Console.WriteLine($"\nBase IPs after clones modified theirs: [{string.Join(",", baseProfile.AllowedIPs)}]");
// Still only 10.0.0.1 and 10.0.0.2 — deep copy worked correctly

// ── C# Records built-in prototype via 'with' ──────────────
Console.WriteLine("\n--- Records + 'with' expression (built-in prototype) ---");

record ServerConfig(string Host, int Port, bool UseTls);

var prodServer = new ServerConfig("prod.api.com", 443, true);
var stagingServer = prodServer with { Host = "staging.api.com", UseTls = false };

Console.WriteLine($"Prod:    {prodServer}");
Console.WriteLine($"Staging: {stagingServer}");
