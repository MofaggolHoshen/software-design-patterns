# 🌉 Bridge Pattern

The Bridge pattern **decouples an abstraction from its implementation** so that the two can vary independently. Instead of using inheritance to extend both abstraction and implementation, Bridge uses composition.

## Intent

> Decouple an abstraction from its implementation so that the two can vary independently.

## Problem

Extending a class hierarchy in two dimensions (e.g., shape × renderer, device × OS) leads to a combinatorial explosion of subclasses. Every combination requires its own subclass: `WindowsCircle`, `LinuxCircle`, `WindowsRectangle`, `LinuxRectangle`, etc. Adding one new shape or one new renderer requires `n` new classes.

### Bad Example

```csharp
// 2 shapes × 2 renderers = 4 classes — adding a 3rd renderer needs 2 more classes
abstract class Shape { public abstract void Draw(); }
class WindowsCircle    : Shape { public override void Draw() => Console.WriteLine("Win  + Circle"); }
class LinuxCircle      : Shape { public override void Draw() => Console.WriteLine("Linux + Circle"); }
class WindowsRectangle : Shape { public override void Draw() => Console.WriteLine("Win  + Rectangle"); }
class LinuxRectangle   : Shape { public override void Draw() => Console.WriteLine("Linux + Rectangle"); }
// Adding "MacCircle" and "MacRectangle" for every future shape
```

### Good Example

```csharp
// ── Implementation side ───────────────────────────────────
interface IRenderer
{
    void RenderCircle(double radius);
    void RenderRectangle(double width, double height);
}

class OpenGLRenderer : IRenderer
{
    public void RenderCircle(double r)       => Console.WriteLine($"  [OpenGL]  Drawing circle r={r}");
    public void RenderRectangle(double w, double h) =>
        Console.WriteLine($"  [OpenGL]  Drawing rect {w}×{h}");
}

class DirectXRenderer : IRenderer
{
    public void RenderCircle(double r)       => Console.WriteLine($"  [DirectX] Drawing circle r={r}");
    public void RenderRectangle(double w, double h) =>
        Console.WriteLine($"  [DirectX] Drawing rect {w}×{h}");
}

class SvgRenderer : IRenderer
{
    public void RenderCircle(double r)       => Console.WriteLine($"  [SVG]     <circle r=\"{r}\"/>");
    public void RenderRectangle(double w, double h) =>
        Console.WriteLine($"  [SVG]     <rect width=\"{w}\" height=\"{h}\"/>");
}

// ── Abstraction side ──────────────────────────────────────
abstract class Shape(IRenderer renderer)
{
    protected IRenderer Renderer { get; } = renderer;
    public abstract void Draw();

    // Renderer can be swapped at runtime
    public Shape WithRenderer(IRenderer r)
    {
        // Return a shallow copy with the new renderer -- pattern variation
        return (Shape)MemberwiseClone(); // simplified; real code passes renderer differently
    }
}

class Circle(double radius, IRenderer renderer) : Shape(renderer)
{
    public override void Draw() => Renderer.RenderCircle(radius);
}

class Rectangle(double width, double height, IRenderer renderer) : Shape(renderer)
{
    public override void Draw() => Renderer.RenderRectangle(width, height);
}

// ── Demo ──────────────────────────────────────────────────
Console.WriteLine("=== Bridge Pattern ===\n");

var renderers = new IRenderer[] { new OpenGLRenderer(), new DirectXRenderer(), new SvgRenderer() };

foreach (var renderer in renderers)
{
    new Circle(5.0, renderer).Draw();
    new Rectangle(4.0, 6.0, renderer).Draw();
}
// Adding a new renderer (WebGPU) = 1 new class; no changes to Circle or Rectangle.
// Adding a new shape (Triangle)  = 1 new class; no changes to any renderer.
```

## Key Takeaways

- Shape and renderer hierarchies vary independently — adding one does not affect the other.
- Composition replaces inheritance-based combinations, eliminating the class explosion.
- The renderer can be swapped at runtime by passing a different implementation.
- Bridge is especially powerful when both dimensions are expected to grow over time.

## When to Use

- You need to extend a class in two independent dimensions (abstraction + implementation).
- You want to swap the implementation at runtime.
- You want to hide implementation details from clients entirely.

## When NOT to Use

- Only one dimension varies — use a simple inheritance hierarchy or Strategy.
- The abstraction and implementation are unlikely to ever change independently.
