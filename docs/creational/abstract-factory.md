# 🏭 Abstract Factory Pattern

The Abstract Factory pattern provides an interface for creating **families of related objects** without specifying their concrete classes. It builds on the Factory Method by grouping related factories into a coherent family, ensuring that products from the same family are always used together.

## Intent

> Provide an interface for creating families of related or dependent objects without specifying their concrete classes.

## Problem

Without an abstract factory, client code must know the exact concrete types to instantiate. When multiple product families must be supported (e.g., Windows vs. macOS UI controls, Light vs. Dark theme components), creation logic is scattered across `if/switch` statements. Adding a new family means updating every creation site — violating the Open/Closed Principle.

### Bad Example

```csharp
// Client is tightly coupled to every concrete product type
class UIRenderer
{
    private readonly string _platform;

    public UIRenderer(string platform) => _platform = platform;

    public IButton CreateButton()
    {
        if (_platform == "Windows") return new WindowsButton();
        if (_platform == "Mac")     return new MacButton();
        throw new NotSupportedException(_platform);
        // Adding "Linux" requires editing this class AND CreateCheckbox below
    }

    public ICheckbox CreateCheckbox()
    {
        if (_platform == "Windows") return new WindowsCheckbox();
        if (_platform == "Mac")     return new MacCheckbox();
        throw new NotSupportedException(_platform);
    }
}
```

### Good Example

```csharp
// ── Abstract products ──────────────────────────────────────
interface IButton   { void Render(); }
interface ICheckbox { void Render(); }

// ── Abstract factory ───────────────────────────────────────
interface IUIFactory
{
    IButton   CreateButton();
    ICheckbox CreateCheckbox();
}

// ── Windows family ─────────────────────────────────────────
class WindowsButton   : IButton   { public void Render() => Console.WriteLine("[Win] Button"); }
class WindowsCheckbox : ICheckbox { public void Render() => Console.WriteLine("[Win] Checkbox"); }

class WindowsFactory : IUIFactory
{
    public IButton   CreateButton()   => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
}

// ── macOS family ───────────────────────────────────────────
class MacButton   : IButton   { public void Render() => Console.WriteLine("[Mac] Button"); }
class MacCheckbox : ICheckbox { public void Render() => Console.WriteLine("[Mac] Checkbox"); }

class MacFactory : IUIFactory
{
    public IButton   CreateButton()   => new MacButton();
    public ICheckbox CreateCheckbox() => new MacCheckbox();
}

// ── Client — depends only on abstractions ──────────────────
class Application(IUIFactory factory)
{
    private readonly IButton   _button   = factory.CreateButton();
    private readonly ICheckbox _checkbox = factory.CreateCheckbox();

    public void Render() { _button.Render(); _checkbox.Render(); }
}

// ── Composition root ───────────────────────────────────────
IUIFactory factory = Environment.OSVersion.Platform == PlatformID.Win32NT
    ? new WindowsFactory()
    : new MacFactory();

new Application(factory).Render();
// Adding "Linux" = add LinuxButton, LinuxCheckbox, LinuxFactory — no existing code changes
```

## Key Takeaways

- Client code never references concrete product classes — only interfaces.
- Adding a new product family means adding one new factory class; no existing code changes (OCP).
- Abstract Factory enforces consistency: you cannot accidentally mix a `WindowsButton` with a `MacCheckbox`.
- Pairs naturally with Dependency Injection — inject `IUIFactory` at the composition root.

## When to Use

- Your system must support multiple families of related products (platforms, themes, providers).
- You want to enforce that products from the same family are always used together.
- You want to isolate object creation from the rest of the application.

## When NOT to Use

- You only have a single product family — a simple Factory Method is sufficient.
- Products per family grow frequently — every new product type requires updating every concrete factory.
