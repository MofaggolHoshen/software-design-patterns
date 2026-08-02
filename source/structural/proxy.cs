// ============================================================
// Proxy Pattern — C# Example
// ============================================================
//
// Intent: Provide a surrogate that controls access to another object.
//
// Three proxy types demonstrated:
//   1. CachingProxy      — lazy caching
//   2. AuthorisedProxy   — access control
//   3. LoggingProxy      — transparent logging
// Key roles:
//   IUserService — Subject interface
//   UserService  — Real Subject
//   *Proxy       — Proxies (all implement IUserService)
// ============================================================

record UserDto(int Id, string Name, string Email);

// ── Subject interface ────────────────────────────────────
interface IUserService
{
    UserDto GetUser(int id);
    UserDto? FindByEmail(string email);
}

// ── Real Subject ─────────────────────────────────────────
class UserService : IUserService
{
    private readonly Dictionary<int, UserDto> _users = new()
    {
        [1] = new UserDto(1, "Alice", "alice@example.com"),
        [2] = new UserDto(2, "Bob", "bob@example.com"),
    };

    public UserDto GetUser(int id)
    {
        Console.WriteLine($"  [DB] Loading user {id}...");
        Thread.Sleep(20); // simulate latency
        return _users.TryGetValue(id, out var u)
            ? u
            : throw new KeyNotFoundException($"User {id} not found.");
    }

    public UserDto? FindByEmail(string email)
    {
        Console.WriteLine($"  [DB] Searching by email '{email}'...");
        return _users.Values.FirstOrDefault(u => u.Email == email);
    }
}

// ── Proxy 1: Caching ─────────────────────────────────────
class CachingProxy(IUserService inner) : IUserService
{
    private readonly Dictionary<int, UserDto> _cache = new();
    private readonly Dictionary<string, UserDto?> _emailCache = new();

    public UserDto GetUser(int id)
    {
        if (_cache.TryGetValue(id, out var hit))
        {
            Console.WriteLine($"  [Cache] HIT user {id}");
            return hit;
        }
        var user = inner.GetUser(id);
        _cache[id] = user;
        return user;
    }

    public UserDto? FindByEmail(string email)
    {
        if (_emailCache.TryGetValue(email, out var hit))
        {
            Console.WriteLine($"  [Cache] HIT email '{email}'");
            return hit;
        }
        var user = inner.FindByEmail(email);
        _emailCache[email] = user;
        return user;
    }
}

// ── Proxy 2: Access control ───────────────────────────────
class AuthorisedProxy(IUserService inner, string role) : IUserService
{
    private static readonly HashSet<string> AllowedRoles = new() { "admin", "manager" };

    private void CheckAccess()
    {
        if (!AllowedRoles.Contains(role))
            throw new UnauthorizedAccessException($"Role '{role}' cannot read user data.");
        Console.WriteLine($"  [Auth] Access granted for role '{role}'");
    }

    public UserDto GetUser(int id) { CheckAccess(); return inner.GetUser(id); }
    public UserDto? FindByEmail(string email) { CheckAccess(); return inner.FindByEmail(email); }
}

// ── Proxy 3: Logging ─────────────────────────────────────
class LoggingProxy(IUserService inner) : IUserService
{
    public UserDto GetUser(int id)
    {
        Console.WriteLine($"  [Log] GetUser({id})");
        var u = inner.GetUser(id);
        Console.WriteLine($"  [Log] GetUser({id}) → {u.Name}");
        return u;
    }

    public UserDto? FindByEmail(string email)
    {
        Console.WriteLine($"  [Log] FindByEmail('{email}')");
        return inner.FindByEmail(email);
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Proxy Pattern ===\n");

// Stack: Log → Auth → Cache → DB
IUserService service =
    new LoggingProxy(
        new AuthorisedProxy(
            new CachingProxy(
                new UserService()),
            role: "admin"));

Console.WriteLine("--- First call (DB hit) ---");
var u1 = service.GetUser(1);
Console.WriteLine($"  Result: {u1.Name} <{u1.Email}>\n");

Console.WriteLine("--- Second call (cache hit) ---");
service.GetUser(1);
Console.WriteLine();

Console.WriteLine("--- Unauthorised access ---");
IUserService restricted =
    new LoggingProxy(
        new AuthorisedProxy(
            new UserService(),
            role: "viewer"));

try { restricted.GetUser(2); }
catch (UnauthorizedAccessException ex) { Console.WriteLine($"  Caught: {ex.Message}"); }
