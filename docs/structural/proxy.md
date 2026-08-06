# 🛡️ Proxy Pattern

The Proxy pattern provides a **surrogate or placeholder** for another object to control access to it. The proxy implements the same interface as the real subject and intercepts calls, adding lazy initialisation, access control, logging, caching, or other concerns.

## Intent

> Provide a surrogate or placeholder for another object to control access to it.

## Problem

When you need cross-cutting concerns (access control, lazy loading, logging, caching) but cannot or should not modify the real subject class directly, adding that logic into every call site duplicates code and violates SRP.

### Bad Example

```csharp
class UserService
{
    public UserDto GetUser(int id)
    {
        // Real expensive call
        return new UserDto(id, "Alice");
    }
}

class OrderController(UserService userService, string currentUser)
{
    public void ShowProfile(int id)
    {
        // Access check, logging, caching all duplicated at every call site
        if (currentUser != "admin") throw new UnauthorizedAccessException();
        Console.WriteLine($"Fetching user {id}...");
        var user = userService.GetUser(id);
        Console.WriteLine($"Got: {user.Name}");
    }
}
```

### Good Example

```csharp
record UserDto(int Id, string Name);

// ── Subject interface ─────────────────────────────────────
interface IUserService
{
    UserDto GetUser(int id);
}

// ── Real Subject ──────────────────────────────────────────
class UserService : IUserService
{
    public UserDto GetUser(int id)
    {
        Console.WriteLine($"  [DB] Fetching user {id} from database...");
        Thread.Sleep(50); // simulate latency
        return new UserDto(id, id == 1 ? "Alice" : "Bob");
    }
}

// ── Proxy 1: Caching Proxy ────────────────────────────────
class CachingUserServiceProxy(IUserService inner) : IUserService
{
    private readonly Dictionary<int, UserDto> _cache = new();

    public UserDto GetUser(int id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            Console.WriteLine($"  [Cache] HIT for user {id}");
            return cached;
        }
        var user = inner.GetUser(id);
        _cache[id] = user;
        return user;
    }
}

// ── Proxy 2: Access Control Proxy ────────────────────────
class AuthorisedUserServiceProxy(IUserService inner, string role) : IUserService
{
    public UserDto GetUser(int id)
    {
        if (role != "admin" && role != "manager")
            throw new UnauthorizedAccessException($"Role '{role}' cannot read user profiles.");
        Console.WriteLine($"  [Auth]  Access granted for role '{role}'");
        return inner.GetUser(id);
    }
}

// ── Proxy 3: Logging Proxy ────────────────────────────────
class LoggingUserServiceProxy(IUserService inner) : IUserService
{
    public UserDto GetUser(int id)
    {
        Console.WriteLine($"  [Log]   GetUser({id}) called at {DateTime.UtcNow:HH:mm:ss.fff}");
        var result = inner.GetUser(id);
        Console.WriteLine($"  [Log]   GetUser({id}) returned '{result.Name}'");
        return result;
    }
}

// ── Compose proxies ───────────────────────────────────────
Console.WriteLine("=== Proxy Pattern ===\n");

IUserService service =
    new LoggingUserServiceProxy(
        new AuthorisedUserServiceProxy(
            new CachingUserServiceProxy(
                new UserService()),
            role: "admin"));

Console.WriteLine("--- First call (cache miss) ---");
var user = service.GetUser(1);
Console.WriteLine($"  Result: {user.Name}\n");

Console.WriteLine("--- Second call (cache hit) ---");
user = service.GetUser(1);
Console.WriteLine($"  Result: {user.Name}\n");

Console.WriteLine("--- Unauthorised access ---");
IUserService restricted =
    new LoggingUserServiceProxy(
        new AuthorisedUserServiceProxy(
            new UserService(),
            role: "viewer"));

try { restricted.GetUser(2); }
catch (UnauthorizedAccessException ex) { Console.WriteLine($"  Caught: {ex.Message}"); }
```

## Key Takeaways

- Proxies are transparent to the client — they implement the same interface as the real subject.
- Cross-cutting concerns (auth, cache, log) are contained in proxy classes, not scattered across call sites.
- Multiple proxies can be stacked (chain of responsibility via composition).
- .NET `DispatchProxy` and Castle DynamicProxy generate proxies at runtime via reflection.

## When to Use

- You need to add access control, caching, logging, or lazy initialisation without touching the real subject.
- You want to delay expensive object creation until it is actually needed (virtual proxy).
- You need to control remote object access (remote proxy / gRPC channel).

## When NOT to Use

- The real subject is simple and adding a proxy layer adds unnecessary indirection.
- You already use AOP (Aspect-Oriented Programming) or DI middleware for cross-cutting concerns.
