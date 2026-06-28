// ============================================================
// Visitor Pattern — C# Example
// ============================================================
//
// Intent: Separate an algorithm from the object structure it
// operates on, letting you add new operations without modifying
// the element classes.
//
// Key roles:
//   IShape           — Element interface (Accept)
//   Circle, Rectangle, Triangle — Concrete Elements
//   IShapeVisitor    — Visitor interface (one Visit per element type)
//   AreaVisitor      — Concrete Visitor
//   SvgExportVisitor — Concrete Visitor
//   PerimeterVisitor — Concrete Visitor (added without touching element classes)
// ============================================================

// ── Element interface ────────────────────────────────────
interface IShape
{
    void Accept(IShapeVisitor visitor);
    string Name { get; }
}

// ── Visitor interface ────────────────────────────────────
interface IShapeVisitor
{
    void Visit(Circle circle);
    void Visit(Rectangle rectangle);
    void Visit(Triangle triangle);
}

// ── Concrete Elements ────────────────────────────────────
class Circle(double radius) : IShape
{
    public double Radius => radius;
    public string Name => $"Circle(r={Radius})";
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

class Rectangle(double width, double height) : IShape
{
    public double Width => width;
    public double Height => height;
    public string Name => $"Rectangle({Width}×{Height})";
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

class Triangle(double @base, double height) : IShape
{
    public double Base => @base;
    public double Height => height;
    public string Name => $"Triangle(b={Base}, h={Height})";
    public void Accept(IShapeVisitor v) => v.Visit(this);
}

// ── Visitor 1: Area ──────────────────────────────────────
class AreaVisitor : IShapeVisitor
{
    private double _total;
    public double TotalArea => _total;

    public void Visit(Circle c) => _total += Math.PI * c.Radius * c.Radius;
    public void Visit(Rectangle r) => _total += r.Width * r.Height;
    public void Visit(Triangle t) => _total += 0.5 * t.Base * t.Height;
}

// ── Visitor 2: SVG export ────────────────────────────────
class SvgExportVisitor : IShapeVisitor
{
    private readonly System.Text.StringBuilder _sb = new("<svg>\n");
    public string Svg => _sb.Append("</svg>").ToString();

    public void Visit(Circle c) => _sb.AppendLine($"  <circle r=\"{c.Radius}\"/>");
    public void Visit(Rectangle r) => _sb.AppendLine($"  <rect width=\"{r.Width}\" height=\"{r.Height}\"/>");
    public void Visit(Triangle t) => _sb.AppendLine($"  <!-- triangle base={t.Base} height={t.Height} -->");
}

// ── Visitor 3: Perimeter ─────────────────────────────────
// Added without touching Circle, Rectangle, or Triangle
class PerimeterVisitor : IShapeVisitor
{
    private readonly System.Text.StringBuilder _sb = new();
    public string Report => _sb.ToString();

    public void Visit(Circle c)
    {
        double p = 2 * Math.PI * c.Radius;
        _sb.AppendLine($"  {c.Name,-30} perimeter = {p:F2}");
    }
    public void Visit(Rectangle r)
    {
        double p = 2 * (r.Width + r.Height);
        _sb.AppendLine($"  {r.Name,-30} perimeter = {p:F2}");
    }
    public void Visit(Triangle t)
    {
        // Assume isosceles right triangle for demo
        double hyp = Math.Sqrt(t.Base * t.Base + t.Height * t.Height);
        double p = t.Base + t.Height + hyp;
        _sb.AppendLine($"  {t.Name,-30} perimeter ≈ {p:F2}");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Visitor Pattern ===\n");

IShape[] shapes = { new Circle(5), new Rectangle(4, 6), new Triangle(3, 8) };

var area = new AreaVisitor();
foreach (var s in shapes) s.Accept(area);
Console.WriteLine($"Total area: {area.TotalArea:F2}\n");

var perim = new PerimeterVisitor();
foreach (var s in shapes) s.Accept(perim);
Console.WriteLine($"Perimeters:\n{perim.Report}");

var svg = new SvgExportVisitor();
foreach (var s in shapes) s.Accept(svg);
Console.WriteLine($"SVG output:\n{svg.Svg}");
