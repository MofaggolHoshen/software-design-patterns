// ============================================================
// Bridge Pattern — C# Example
// ============================================================
//
// Intent: Decouple an abstraction from its implementation so both
// can vary independently — avoiding a combinatorial class explosion.
//
// Key roles:
//   IRenderer      — Implementation interface
//   OpenGLRenderer, DirectXRenderer, SvgRenderer — Concrete Implementations
//   Shape          — Abstraction (holds a reference to IRenderer)
//   Circle, Rectangle — Refined Abstractions
// ============================================================

// ── Implementation interface ──────────────────────────────
interface IRenderer
{
    string Name { get; }
    void RenderCircle(double radius);
    void RenderRectangle(double width, double height);
    void RenderTriangle(double @base, double height);
}

// ── Concrete Implementations ──────────────────────────────
class OpenGLRenderer : IRenderer
{
    public string Name => "OpenGL";
    public void RenderCircle(double r) => Console.WriteLine($"  [{Name}] circle r={r}");
    public void RenderRectangle(double w, double h) => Console.WriteLine($"  [{Name}] rect {w}×{h}");
    public void RenderTriangle(double b, double h) => Console.WriteLine($"  [{Name}] triangle b={b} h={h}");
}

class DirectXRenderer : IRenderer
{
    public string Name => "DirectX";
    public void RenderCircle(double r) => Console.WriteLine($"  [{Name}] circle r={r}");
    public void RenderRectangle(double w, double h) => Console.WriteLine($"  [{Name}] rect {w}×{h}");
    public void RenderTriangle(double b, double h) => Console.WriteLine($"  [{Name}] triangle b={b} h={h}");
}

class SvgRenderer : IRenderer
{
    public string Name => "SVG";
    public void RenderCircle(double r) => Console.WriteLine($"  <circle r=\"{r}\"/>");
    public void RenderRectangle(double w, double h) => Console.WriteLine($"  <rect width=\"{w}\" height=\"{h}\"/>");
    public void RenderTriangle(double b, double h) => Console.WriteLine($"  <!-- triangle b={b} h={h} -->");
}

// ── Abstraction ───────────────────────────────────────────
abstract class Shape(IRenderer renderer)
{
    protected IRenderer Renderer { get; } = renderer;
    public abstract void Draw();
    public abstract double Area { get; }
}

// ── Refined Abstractions ──────────────────────────────────
class Circle(double radius, IRenderer renderer) : Shape(renderer)
{
    public override double Area => Math.PI * radius * radius;
    public override void Draw() => Renderer.RenderCircle(radius);
}

class Rectangle(double width, double height, IRenderer renderer) : Shape(renderer)
{
    public override double Area => width * height;
    public override void Draw() => Renderer.RenderRectangle(width, height);
}

class Triangle(double @base, double triHeight, IRenderer renderer) : Shape(renderer)
{
    public override double Area => 0.5 * @base * triHeight;
    public override void Draw() => Renderer.RenderTriangle(@base, triHeight);
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Bridge Pattern ===\n");

var renderers = new IRenderer[] { new OpenGLRenderer(), new DirectXRenderer(), new SvgRenderer() };

foreach (var renderer in renderers)
{
    Console.WriteLine($"--- {renderer.Name} ---");
    new Circle(5.0, renderer).Draw();
    new Rectangle(4.0, 6.0, renderer).Draw();
    new Triangle(3.0, 8.0, renderer).Draw();
    Console.WriteLine();
}

// Adding WebGPURenderer = 1 new class; Circle/Rectangle/Triangle untouched.
// Adding Ellipse shape   = 1 new class; all renderers untouched.
Console.WriteLine("3 shapes × 3 renderers = 9 combinations from 6 classes (not 9).");
