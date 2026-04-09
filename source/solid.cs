// ============================================================
// SOLID Design Principles — C# Examples
// ============================================================
//
// ┌────────┬──────────────────────────────────────┬──────────────────────────────────────────────────┐
// │ Letter │ Principle                            │ One-line summary                                 │
// ├────────┼──────────────────────────────────────┼──────────────────────────────────────────────────┤
// │   S    │ Single Responsibility Principle (SRP) │ A class should have only one reason to change    │
// │   O    │ Open/Closed Principle          (OCP) │ Open for extension, closed for modification      │
// │   L    │ Liskov Substitution Principle  (LSP) │ Subtypes must be substitutable for their base    │
// │   I    │ Interface Segregation Principle(ISP) │ Don't force clients to depend on unused members  │
// │   D    │ Dependency Inversion Principle (DIP) │ Depend on abstractions, not concrete types       │
// └────────┴──────────────────────────────────────┴──────────────────────────────────────────────────┘

// ─────────────────────────────────────────────────────────────
// S — Single Responsibility Principle (SRP)
// A class should have only one reason to change.
//
// "Reason to change" = the stakeholder or business concern
// that drives a future modification. If two different teams
// (e.g. Finance and DevOps) could separately ask you to
// change the same class, it has more than one responsibility
// and should be split.
// ─────────────────────────────────────────────────────────────

// BAD: OrderServiceBad breaks SRP because it owns two distinct
// responsibilities — business logic (PlaceOrder) and data
// persistence (SaveToDatabase). If the DB schema changes, this
// class must be modified even though the order logic is fine.
// If the business rules change, the DB code is also in scope.
// Two reasons to change → two responsibilities → violates SRP.
class OrderServiceBad
{
    public void PlaceOrder(string item) { /* business logic */ }
    public void SaveToDatabase(string item) { /* DB concern mixed in */ }
}

// GOOD: Each class owns exactly one concern.
//   • OrderService  — knows HOW to place an order (business rules)
//   • OrderRepository — knows HOW to persist data (infrastructure)
// Now a DB change only touches OrderRepository, and a rule change
// only touches OrderService. Each has a single reason to change.
class OrderService
{
    // Depend on OrderRepository via constructor injection so the
    // persistence concern stays encapsulated inside that class.
    private readonly OrderRepository _repo;

    public OrderService(OrderRepository repo) => _repo = repo;

    public void PlaceOrder(string item)
    {
        // Pure business logic: validate, apply rules, then delegate
        // persistence to the repository — don't do it inline here.
        Console.WriteLine($"Order placed for: {item}");
        _repo.Save(item);
    }
}

// OrderRepository's ONLY job is to talk to the data store.
// It doesn't know anything about business rules or validation.
class OrderRepository
{
    public void Save(string item) =>
        Console.WriteLine($"Saving '{item}' to database.");
}


// ─────────────────────────────────────────────────────────────
// O — Open/Closed Principle (OCP)
// Open for extension, closed for modification.
//
// "Open for extension"   → you can add new behaviour.
// "Closed for modification" → you do NOT edit existing, tested code.
//
// The key tool is polymorphism: define a stable abstraction
// (abstract class or interface), then extend it with new
// subclasses instead of adding if/switch branches to existing code.
// ─────────────────────────────────────────────────────────────

// Discount is the stable abstraction. Every pricing rule is a
// subclass of Discount. Adding a new rule = adding a new subclass;
// nothing already written needs to change.
abstract class Discount
{
    // Subclasses implement exactly HOW to adjust the price.
    // The rest of the system never cares which subclass it receives.
    public abstract decimal Apply(decimal price);
}

// No discount: pass the price through unchanged.
// Useful as a null-object to avoid null checks in callers.
class NoDiscount : Discount
{
    public override decimal Apply(decimal price) => price;
}

// Percentage-based discount: deduct a configurable % from the price.
// The percentage is injected at construction time so the class is
// reusable across different percentage values (10%, 20%, etc.).
class PercentageDiscount : Discount
{
    private readonly decimal _percent;
    public PercentageDiscount(decimal percent) => _percent = percent;

    public override decimal Apply(decimal price) =>
        price - price * _percent / 100;
}

// Seasonal discount: a hard-coded 15% reduction tied to a season.
// Adding this class is the ONLY change required — PriceCalculator
// below never had to be touched. That is OCP in action.
class SeasonalDiscount : Discount
{
    public override decimal Apply(decimal price) => price * 0.85m; // 15% off
}

// PriceCalculator is "closed for modification": it accepts any
// Discount via the abstraction. You can introduce a new discount
// type tomorrow and this class requires zero changes.
class PriceCalculator
{
    // No modification needed when adding a new discount type
    public decimal Calculate(decimal price, Discount discount) =>
        discount.Apply(price);
}


// ─────────────────────────────────────────────────────────────
// L — Liskov Substitution Principle (LSP)
// Subtypes must be substitutable for their base types.
//
// Formally: if S is a subtype of T, then any code that works
// correctly with a T must also work correctly with an S —
// without knowing or caring which concrete type it received.
//
// A common violation: Square inheriting from Rectangle and
// overriding setters so Width = Height. Code that sets Width
// and Height independently then gets wrong area results when
// given a Square. The subtype broke the caller's expectation.
//
// The fix here: both Rectangle and Circle inherit from Shape
// and honour its contract (Area() returns the correct area).
// Neither subtype surprises the caller.
// ─────────────────────────────────────────────────────────────

// Shape defines the contract: every shape can compute its area.
// Concrete implementations MUST honour this without throwing
// unexpected exceptions or returning nonsensical values.
abstract class Shape
{
    public abstract double Area();
}

// Rectangle satisfies the contract: Area = Width × Height.
// Nothing about this class breaks a caller's expectations.
class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }

    public override double Area() => Width * Height;
}

// Circle satisfies the same contract using a different formula.
// A caller that works with Shape works equally well with Circle
// — it can be swapped in anywhere Rectangle is used.
class Circle : Shape
{
    public double Radius { get; set; }

    // π r² — standard area formula; no surprises.
    public override double Area() => Math.PI * Radius * Radius;
}

// AreaPrinter accepts Shape (the base type). It can receive a
// Rectangle, Circle, or any future Shape subclass and still
// produce the correct output — because LSP is satisfied.
static class AreaPrinter
{
    public static void Print(Shape shape) =>
        Console.WriteLine($"Area: {shape.Area():F2}");
}


// ─────────────────────────────────────────────────────────────
// I — Interface Segregation Principle (ISP)
// Clients should not be forced to depend on interfaces they
// do not use.
//
// A "fat" interface groups unrelated methods together. Any
// implementor that doesn't need some of those methods is
// forced to provide stub implementations (throw, return null,
// or do nothing). These stubs are noise and can mask bugs.
//
// The fix: split fat interfaces into small, focused ones.
// Implementors pick only the contracts that make sense for them.
// ─────────────────────────────────────────────────────────────

// BAD: IWorkerBad lumps together three completely different
// concerns. A RobotWorker doesn't eat or sleep, yet it would
// be forced to implement those methods — misleading and fragile.
interface IWorkerBad
{
    void Work();
    void Eat();    // irrelevant for a robot
    void Sleep();  // irrelevant for a robot
}

// GOOD: three focused interfaces, each with a single concern.
// Callers that only need working behaviour depend ONLY on
// IWorkable — they never even see Eat() or Sleep().
interface IWorkable { void Work(); }
interface IEatable { void Eat(); }
interface ISleepable { void Sleep(); }

// A human needs all three behaviours — so it implements all three.
// Each interface is implemented for a genuine reason.
class HumanWorker : IWorkable, IEatable, ISleepable
{
    public void Work() => Console.WriteLine("Human working.");
    public void Eat() => Console.WriteLine("Human eating.");
    public void Sleep() => Console.WriteLine("Human sleeping.");
}

// A robot only works — it implements only IWorkable.
// No forced stubs. The type expresses its capabilities accurately.
class RobotWorker : IWorkable
{
    public void Work() => Console.WriteLine("Robot working.");
}


// ─────────────────────────────────────────────────────────────
// D — Dependency Inversion Principle (DIP)
// High-level modules should not depend on low-level modules.
// Both should depend on abstractions.
// Abstractions should not depend on details;
// details should depend on abstractions.
//
// Without DIP: NotificationService would directly instantiate
// EmailSender. Switching to SMS later would require modifying
// NotificationService — a high-level business class — purely
// because an infrastructure detail changed.
//
// With DIP: NotificationService only knows about IMessageSender.
// The concrete sender (Email, SMS, Push…) is decided outside the
// class and injected in — typically by a DI container at startup.
// ─────────────────────────────────────────────────────────────

// IMessageSender is the abstraction (the "stable contract").
// High-level code depends on this; low-level classes implement it.
interface IMessageSender
{
    void Send(string message);
}

// EmailSender is a low-level detail — it knows how to talk to
// an email server. NotificationService doesn't need to know this.
class EmailSender : IMessageSender
{
    public void Send(string message) =>
        Console.WriteLine($"Email: {message}");
}

// SmsSender is another low-level detail. Swapping Email for SMS
// is purely an infrastructure decision; business logic is untouched.
class SmsSender : IMessageSender
{
    public void Send(string message) =>
        Console.WriteLine($"SMS: {message}");
}

// NotificationService is the high-level module. It contains the
// business rule ("send a notification") but has zero knowledge of
// email protocols, SMS gateways, or any transport detail.
// The concrete IMessageSender is injected at construction time,
// which means it can be swapped or mocked freely in unit tests.
class NotificationService
{
    private readonly IMessageSender _sender;

    // Constructor injection: the caller decides WHICH sender to use.
    // NotificationService never calls `new EmailSender()` — doing so
    // would re-introduce a concrete dependency and break DIP.
    public NotificationService(IMessageSender sender) => _sender = sender;

    public void Notify(string message) => _sender.Send(message);
}


// ─────────────────────────────────────────────────────────────
// Usage
// Exercises each principle with realistic values so you can
// run this file directly (top-level statements, .NET 6+).
// ─────────────────────────────────────────────────────────────

// SRP — the service delegates persistence; both classes do one job.
var repo = new OrderRepository();
var orders = new OrderService(repo);
orders.PlaceOrder("Laptop");

// OCP — pass a new discount type without changing PriceCalculator.
var calculator = new PriceCalculator();
Console.WriteLine(calculator.Calculate(200m, new PercentageDiscount(10)));  // 180

// LSP — AreaPrinter works correctly for every Shape subtype.
AreaPrinter.Print(new Rectangle { Width = 4, Height = 5 });  // Area: 20.00
AreaPrinter.Print(new Circle { Radius = 3 });                 // Area: 28.27

// ISP — RobotWorker only exposes the interface it actually uses.
IWorkable robot = new RobotWorker();
robot.Work();

// DIP — swap the sender without touching NotificationService.
var notifier = new NotificationService(new EmailSender());
notifier.Notify("Your order has shipped!");

notifier = new NotificationService(new SmsSender());
notifier.Notify("One-time password: 4821");
