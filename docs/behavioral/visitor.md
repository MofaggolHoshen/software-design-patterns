# 🧳 Visitor Pattern

The Visitor pattern lets you **add new operations to an object structure** without modifying the classes in that structure. The operation logic is moved into a separate visitor object, and each element in the structure "accepts" the visitor.

## Intent

> Represent an operation to be performed on elements of an object structure. Visitor lets you define a new operation without changing the classes of the elements on which it operates.

## Problem

When you need to perform many unrelated operations across a stable hierarchy of types, adding each operation as a method to every class pollutes the hierarchy with unrelated concerns. Alternatively, using `is`/`as` type checks in the operation code breaks the Open/Closed Principle.

### Bad Example

```csharp
// Every new operation (tax, shipping, export) requires editing all shape classes
class Circle   { public double Radius { get; init; } public double Area()    => Math.PI * Radius * Radius; }
class Rectangle{ public double W { get; init; } public double H { get; init; } public double Area() => W * H; }

// To add "Perimeter" we must edit both Circle and Rectangle — OCP violation
// To add "ExportSvg" we must edit both again, and so on
```

### Good Example

```csharp
// ── Element interface — accepts any visitor ───────────────
interface IShape
{
    void Accept(IShapeVisitor visitor);
}

// ── Visitor interface — one method per concrete element ───
interface IShapeVisitor
{
    void Visit(Circle circle);
    void Visit(Rectangle rectangle);
    void Visit(Triangle triangle);
}

// ── Concrete Elements ─────────────────────────────────────
class Circle(double radius) : IShape
{
    public double Radius => radius;
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

class Rectangle(double width, double height) : IShape
{
    public double Width  => width;
    public double Height => height;
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

class Triangle(double @base, double height) : IShape
{
    public double Base   => @base;
    public double Height => height;
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

// ── Concrete Visitor 1: Area calculation ──────────────────
class AreaVisitor : IShapeVisitor
{
    public double TotalArea { get; private set; }

    public void Visit(Circle r)    => TotalArea += Math.PI * r.Radius * r.Radius;
    public void Visit(Rectangle r) => TotalArea += r.Width * r.Height;
    public void Visit(Triangle r)  => TotalArea += 0.5 * r.Base * r.Height;
}

// ── Concrete Visitor 2: SVG export ────────────────────────
class SvgExportVisitor : IShapeVisitor
{
    private readonly System.Text.StringBuilder _sb = new();

    public void Visit(Circle r)    => _sb.AppendLine($"<circle r=\"{r.Radius}\"/>");
    public void Visit(Rectangle r) => _sb.AppendLine($"<rect width=\"{r.Width}\" height=\"{r.Height}\"/>");
    public void Visit(Triangle r)  => _sb.AppendLine($"<polygon points=\"triangle base={r.Base}\"/>");
    public string Svg              => _sb.ToString();
}

// ── Demo ──────────────────────────────────────────────────
IShape[] shapes = { new Circle(5), new Rectangle(4, 6), new Triangle(3, 8) };

var area = new AreaVisitor();
foreach (var shape in shapes) shape.Accept(area);
Console.WriteLine($"Total area: {area.TotalArea:F2}");

var svg = new SvgExportVisitor();
foreach (var shape in shapes) shape.Accept(svg);
Console.WriteLine($"SVG:\n{svg.Svg}");

// Adding a new operation (e.g., PerimeterVisitor) = add one class. No element changes.
```

## Key Takeaways

- New operations are added without touching any element classes — OCP satisfied.
- All logic for an operation is centralised in one visitor class.
- `Accept(visitor)` is the "double dispatch" mechanism — ensures the correct overload is called without `is`/`as` checks.
- The element hierarchy must be stable; adding a new element type requires updating every visitor.

## When to Use

- You need many distinct and unrelated operations on a stable object structure.
- An operation must work across a hierarchy without polluting element classes with unrelated logic.
- You want compiler-enforced exhaustiveness when adding element types.

## When NOT to Use

- The element hierarchy changes frequently — adding a new element type breaks all visitors.
- The operations are closely related to the elements' data (high cohesion) — keep them as methods on the elements.
