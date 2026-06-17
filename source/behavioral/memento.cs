// ============================================================
// Memento Pattern — C# Example
// ============================================================
//
// Intent: Capture and restore an object's internal state without
// violating encapsulation.
//
// Key roles:
//   CanvasMemento    — Memento (opaque snapshot)
//   DrawingCanvas    — Originator (creates/restores mementos)
//   UndoManager      — Caretaker (manages the undo stack)
// ============================================================

// ── Memento — opaque to the outside world ────────────────
class CanvasMemento
{
    internal List<string> Shapes { get; }
    internal string Color { get; }
    internal int ZoomLevel { get; }
    internal DateTime Timestamp { get; }

    internal CanvasMemento(List<string> shapes, string color, int zoom)
    {
        Shapes = new List<string>(shapes);   // deep copy
        Color = color;
        ZoomLevel = zoom;
        Timestamp = DateTime.UtcNow;
    }
}

// ── Originator ──────────────────────────────────────────
class DrawingCanvas
{
    private List<string> _shapes = new();
    private string _color = "Black";
    private int _zoomLevel = 100;

    public void AddShape(string shape)
    {
        _shapes.Add(shape);
        Console.WriteLine($"  + Shape: {shape}");
    }

    public void SetColor(string color)
    {
        _color = color;
        Console.WriteLine($"  Color → {color}");
    }

    public void SetZoom(int zoom)
    {
        _zoomLevel = zoom;
        Console.WriteLine($"  Zoom  → {zoom}%");
    }

    public void PrintState() =>
        Console.WriteLine($"  State: shapes=[{string.Join(",", _shapes)}] color={_color} zoom={_zoomLevel}%");

    // Save current state as an immutable snapshot
    public CanvasMemento Save()
    {
        Console.WriteLine("  [Saved snapshot]");
        return new CanvasMemento(_shapes, _color, _zoomLevel);
    }

    // Restore from a previously saved snapshot
    public void Restore(CanvasMemento m)
    {
        _shapes = new List<string>(m.Shapes);
        _color = m.Color;
        _zoomLevel = m.ZoomLevel;
        Console.WriteLine($"  [Restored @ {m.Timestamp:HH:mm:ss.fff}]");
    }
}

// ── Caretaker ────────────────────────────────────────────
class UndoManager
{
    private readonly Stack<CanvasMemento> _history = new();

    public void Save(DrawingCanvas canvas) => _history.Push(canvas.Save());
    public bool CanUndo => _history.Count > 0;

    public void Undo(DrawingCanvas canvas)
    {
        if (_history.TryPop(out var m))
            canvas.Restore(m);
        else
            Console.WriteLine("  Nothing to undo.");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Memento Pattern ===\n");

var canvas = new DrawingCanvas();
var undo = new UndoManager();

undo.Save(canvas);                // snapshot 1 — empty

canvas.AddShape("Circle");
canvas.SetColor("Blue");
undo.Save(canvas);                // snapshot 2

canvas.AddShape("Rectangle");
canvas.SetZoom(150);
undo.Save(canvas);                // snapshot 3

canvas.AddShape("Triangle");
Console.WriteLine("\nCurrent state:");
canvas.PrintState();

Console.WriteLine("\n--- Undo 1 ---");
undo.Undo(canvas);
canvas.PrintState();

Console.WriteLine("\n--- Undo 2 ---");
undo.Undo(canvas);
canvas.PrintState();

Console.WriteLine("\n--- Undo 3 ---");
undo.Undo(canvas);
canvas.PrintState();

Console.WriteLine("\n--- Nothing left ---");
undo.Undo(canvas);
