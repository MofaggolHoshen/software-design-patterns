# SOLID Design Principles in C#

SOLID is an acronym for five object-oriented design principles introduced by Robert C. Martin ("Uncle Bob"). Following these principles leads to code that is easier to maintain, extend, and test.

| Letter | Principle | One-line summary |
|--------|-----------|-----------------|
| **S** | [Single Responsibility](#s--single-responsibility-principle-srp) | A class should have only one reason to change |
| **O** | [Open/Closed](#o--openclosed-principle-ocp) | Open for extension, closed for modification |
| **L** | [Liskov Substitution](#l--liskov-substitution-principle-lsp) | Subtypes must be substitutable for their base types |
| **I** | [Interface Segregation](#i--interface-segregation-principle-isp) | Clients should not depend on interfaces they do not use |
| **D** | [Dependency Inversion](#d--dependency-inversion-principle-dip) | Depend on abstractions, not on concrete implementations |

---

## S — Single Responsibility Principle (SRP)

### What it means

Every class (or module) should have **exactly one responsibility** — one reason to change. When a class handles multiple concerns, changing one concern risks breaking the other.

### Why it matters

- Easier to understand — each class does one thing
- Easier to test — focused classes have fewer dependencies
- Changes in one area don't ripple into unrelated areas

### Bad example

```csharp
// OrderServiceBad mixes business logic with persistence — two reasons to change
class OrderServiceBad
{
    public void PlaceOrder(string item) { /* business logic */ }
    public void SaveToDatabase(string item) { /* DB concern mixed in */ }
}
```

If the database technology changes, you're forced to touch the class that holds your order logic.

### Good example

```csharp
// Business logic only
class OrderService
{
    private readonly OrderRepository _repo;

    public OrderService(OrderRepository repo) => _repo = repo;

    public void PlaceOrder(string item)
    {
        Console.WriteLine($"Order placed for: {item}");
        _repo.Save(item);
    }
}

// Persistence only
class OrderRepository
{
    public void Save(string item) =>
        Console.WriteLine($"Saving '{item}' to database.");
}
```

Now `OrderService` is isolated from database concerns. Swapping the repository (e.g., SQL → NoSQL) doesn't touch order logic.

---

## O — Open/Closed Principle (OCP)

### What it means

A class should be **open for extension** (you can add new behaviour) but **closed for modification** (you don't change existing, tested code to do so).

### Why it matters

- Adding features cannot accidentally break existing functionality
- Each new behaviour is self-contained in its own class
- Promotes polymorphism over long `if`/`switch` chains

### Bad example

```csharp
class PriceCalculatorBad
{
    public decimal Calculate(decimal price, string discountType)
    {
        // Must modify this method every time a new discount is added
        if (discountType == "percentage") return price * 0.9m;
        if (discountType == "seasonal")  return price * 0.85m;
        return price;
    }
}
```

### Good example

```csharp
// Base abstraction — closed for modification
abstract class Discount
{
    public abstract decimal Apply(decimal price);
}

// Each new discount type is an extension — no existing code changes
class NoDiscount : Discount
{
    public override decimal Apply(decimal price) => price;
}

class PercentageDiscount : Discount
{
    private readonly decimal _percent;
    public PercentageDiscount(decimal percent) => _percent = percent;

    public override decimal Apply(decimal price) =>
        price - price * _percent / 100;
}

class SeasonalDiscount : Discount
{
    public override decimal Apply(decimal price) => price * 0.85m; // 15% off
}

// PriceCalculator never changes when a new discount type is introduced
class PriceCalculator
{
    public decimal Calculate(decimal price, Discount discount) =>
        discount.Apply(price);
}
```

**Usage:**
```csharp
var calculator = new PriceCalculator();
Console.WriteLine(calculator.Calculate(200m, new PercentageDiscount(10))); // 180
Console.WriteLine(calculator.Calculate(200m, new SeasonalDiscount()));     // 170
```

---

## L — Liskov Substitution Principle (LSP)

### What it means

If `S` is a subtype of `T`, then objects of type `T` may be replaced with objects of type `S` **without altering the correctness of the program**. Put simply: a derived class must honour the contract of its base class.

### Why it matters

- Prevents unexpected behaviours when using polymorphism
- Ensures that inheritance models a true "is-a" relationship
- Makes code that works with base types reliably work with any subtype

### Bad example (LSP violation)

```csharp
class Bird
{
    public virtual void Fly() => Console.WriteLine("Flying");
}

// Penguin is a Bird but cannot fly — overriding Fly with an exception breaks LSP
class Penguin : Bird
{
    public override void Fly() => throw new NotSupportedException("Penguins can't fly!");
}
```

Any code that calls `bird.Fly()` will crash when it receives a `Penguin`.

### Good example

```csharp
abstract class Shape
{
    public abstract double Area();
}

class Rectangle : Shape
{
    public double Width  { get; set; }
    public double Height { get; set; }

    public override double Area() => Width * Height;
}

class Circle : Shape
{
    public double Radius { get; set; }

    public override double Area() => Math.PI * Radius * Radius;
}

// Works correctly with any Shape subtype — LSP satisfied
static class AreaPrinter
{
    public static void Print(Shape shape) =>
        Console.WriteLine($"Area: {shape.Area():F2}");
}
```

**Usage:**
```csharp
AreaPrinter.Print(new Rectangle { Width = 4, Height = 5 }); // Area: 20.00
AreaPrinter.Print(new Circle { Radius = 3 });               // Area: 28.27
```

---

## I — Interface Segregation Principle (ISP)

### What it means

**No client should be forced to depend on methods it does not use.** Prefer many small, focused interfaces over one large, general-purpose interface.

### Why it matters

- Classes only implement what they actually need
- Reduces coupling — a change to one responsibility doesn't force unrelated classes to recompile/change
- Interfaces become documentation of intent

### Bad example

```csharp
// One fat interface forces every implementor to define all three methods
interface IWorkerBad
{
    void Work();
    void Eat();
    void Sleep();
}

// A robot doesn't eat or sleep — forced to implement methods that make no sense
class RobotWorkerBad : IWorkerBad
{
    public void Work()  => Console.WriteLine("Robot working.");
    public void Eat()   => throw new NotImplementedException();
    public void Sleep() => throw new NotImplementedException();
}
```

### Good example

```csharp
// Small, focused interfaces
interface IWorkable  { void Work(); }
interface IEatable   { void Eat(); }
interface ISleepable { void Sleep(); }

// Human implements everything it genuinely does
class HumanWorker : IWorkable, IEatable, ISleepable
{
    public void Work()  => Console.WriteLine("Human working.");
    public void Eat()   => Console.WriteLine("Human eating.");
    public void Sleep() => Console.WriteLine("Human sleeping.");
}

// Robot only implements what applies to it
class RobotWorker : IWorkable
{
    public void Work() => Console.WriteLine("Robot working.");
}
```

**Usage:**
```csharp
IWorkable robot = new RobotWorker();
robot.Work(); // Robot working.
```

---

## D — Dependency Inversion Principle (DIP)

### What it means

1. **High-level modules** should not depend on **low-level modules** — both should depend on **abstractions**.
2. **Abstractions** should not depend on **details** — details should depend on abstractions.

In practice: inject dependencies through interfaces, not concrete types.

### Why it matters

- High-level business logic is decoupled from infrastructure concerns (databases, email, SMS, etc.)
- Implementations can be swapped (or mocked in tests) without touching the consuming class
- Enables true unit testing — pass a fake/mock instead of the real dependency

### Bad example

```csharp
// NotificationServiceBad is tightly coupled to EmailSender
class NotificationServiceBad
{
    private readonly EmailSender _sender = new EmailSender(); // concrete dependency!

    public void Notify(string message) => _sender.Send(message);
}
```

Switching to SMS requires modifying `NotificationServiceBad`.

### Good example

```csharp
// Abstraction
interface IMessageSender
{
    void Send(string message);
}

// Low-level detail #1
class EmailSender : IMessageSender
{
    public void Send(string message) =>
        Console.WriteLine($"Email: {message}");
}

// Low-level detail #2
class SmsSender : IMessageSender
{
    public void Send(string message) =>
        Console.WriteLine($"SMS: {message}");
}

// High-level module depends only on the abstraction
class NotificationService
{
    private readonly IMessageSender _sender;

    public NotificationService(IMessageSender sender) => _sender = sender;

    public void Notify(string message) => _sender.Send(message);
}
```

**Usage:**
```csharp
// Swap implementations without changing NotificationService
var emailNotifier = new NotificationService(new EmailSender());
emailNotifier.Notify("Your order has shipped!"); // Email: Your order has shipped!

var smsNotifier = new NotificationService(new SmsSender());
smsNotifier.Notify("One-time password: 4821");   // SMS: One-time password: 4821
```

In real applications, the concrete type is wired up by a **dependency injection (DI) container** (e.g., `Microsoft.Extensions.DependencyInjection`), keeping every class completely unaware of which implementation is used at runtime.

---

## Quick Reference

| Principle | Problem it solves | Key technique |
|-----------|------------------|---------------|
| SRP | Classes that change for multiple reasons | Split into focused classes |
| OCP | Breaking existing code when adding features | Extend via inheritance / composition |
| LSP | Subtypes that violate the base class contract | Honour the base contract in every subtype |
| ISP | Forcing classes to implement unused members | Split large interfaces into small ones |
| DIP | High-level code tightly coupled to low-level code | Program to interfaces; inject dependencies |

> **Source code:** [`source/design-pattern/solid.cs`](../source/design-pattern/solid.cs)
