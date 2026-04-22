// ============================================================
// Abstract Factory Pattern — C# Example
// ============================================================
//
// Intent: Provide an interface for creating families of related
// objects without specifying their concrete classes.
//
// Key roles:
//   IUIFactory        — Abstract Factory interface
//   WindowsFactory    — Concrete Factory (Windows family)
//   MacFactory        — Concrete Factory (macOS family)
//   IButton/ICheckbox — Abstract Products
//   Application       — Client (depends only on abstractions)
//
// Benefits:
//   - Enforces consistency among products of the same family.
//   - Makes it easy to add new product families without changing existing code.
// Drawbacks:
//   - Can be overkill for simple applications with few products.
// Usage:
//   - UI toolkits that support multiple platforms.
// 
// Run:
//   dotnet run .\source\creational\abstract-factory.cs
// ============================================================

// ── Abstract products ──────────────────────────────────────
interface IButton { void Render(); }
interface ICheckbox { void Render(); }

// ── Abstract factory ───────────────────────────────────────
interface IUIFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
}

// ── Windows product family ─────────────────────────────────
class WindowsButton : IButton
{
    public void Render() => Console.WriteLine("[Windows] Rendering a flat, rectangular button");
}

class WindowsCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("[Windows] Rendering a square checkbox");
}

class WindowsFactory : IUIFactory
{
    public IButton CreateButton() => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
}

// ── macOS product family ───────────────────────────────────
class MacButton : IButton
{
    public void Render() => Console.WriteLine("[Mac] Rendering a rounded button");
}

class MacCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("[Mac] Rendering a circular checkbox");
}

class MacFactory : IUIFactory
{
    public IButton CreateButton() => new MacButton();
    public ICheckbox CreateCheckbox() => new MacCheckbox();
}

// ── Client ─────────────────────────────────────────────────
// Application never references concrete types — it works with
// whatever family the factory produces.
class Application(IUIFactory factory)
{
    private readonly IButton _button = factory.CreateButton();
    private readonly ICheckbox _checkbox = factory.CreateCheckbox();

    public void RenderUI()
    {
        _button.Render();
        _checkbox.Render();
    }
}

// ── Demo ───────────────────────────────────────────────────
class Program
{
    static void Main()
    {
        Console.WriteLine("=== Abstract Factory Pattern ===\n");

        Console.WriteLine("--- Windows UI ---");
        new Application(new WindowsFactory()).RenderUI();

        Console.WriteLine("\n--- macOS UI ---");
        new Application(new MacFactory()).RenderUI();

        // To add a "Linux" family: create LinuxButton, LinuxCheckbox, LinuxFactory.
        // No existing code needs to change.
        Console.WriteLine("\nAdding a new family requires no changes to Application or existing factories.");

        // Simulate user choice of platform
        Console.WriteLine("Select platform (1: Windows, 2: macOS): ");
        var choice = Console.ReadLine();

        IUIFactory factory = choice switch
        {
            "1" => new WindowsFactory(),
            "2" => new MacFactory(),
            _ => throw new ArgumentException("Invalid choice")
        };

        var app = new Application(factory);
        app.RenderUI();


    }
}
