# 💾 Memento Pattern

The Memento pattern captures and externalises an object's **internal state** without violating encapsulation, so the object can be restored to that state later. It is the classic underpinning of undo/redo systems and snapshot-based persistence.

## Intent

> Without violating encapsulation, capture and externalise an object's internal state so that the object can be restored to this state later.

## Problem

Implementing undo requires saving state before each change. If state is saved externally by exposing internal fields, encapsulation is broken and the external class becomes tightly coupled to the object's internals. Any change to private state requires updating all callers that copy it.

### Bad Example

```csharp
class DrawingCanvas
{
    // All fields are public to allow "undo" snapshots from outside — breaks encapsulation
    public List<string> Shapes  = new();
    public string       Color   = "Black";
    public int          ZoomLevel = 100;
}

// Caller must know and manually copy every internal field
var backup = new DrawingCanvas
{
    Shapes    = new List<string>(canvas.Shapes),
    Color     = canvas.Color,
    ZoomLevel = canvas.ZoomLevel
};
```

### Good Example

```csharp
// ── Memento — opaque snapshot ────────────────────────────
class CanvasMemento
{
    // Internal state exposed only to DrawingCanvas (via nested class or internal access)
    internal List<string> Shapes    { get; }
    internal string       Color     { get; }
    internal int          ZoomLevel { get; }

    internal CanvasMemento(List<string> shapes, string color, int zoom)
    {
        Shapes    = new List<string>(shapes);
        Color     = color;
        ZoomLevel = zoom;
    }
}

// ── Originator ────────────────────────────────────────────
class DrawingCanvas
{
    private List<string> _shapes    = new();
    private string       _color     = "Black";
    private int          _zoomLevel = 100;

    public void AddShape(string shape)  { _shapes.Add(shape); Console.WriteLine($"  Added: {shape}"); }
    public void SetColor(string color)  { _color = color;     Console.WriteLine($"  Color: {color}"); }
    public void SetZoom(int zoom)       { _zoomLevel = zoom;  Console.WriteLine($"  Zoom:  {zoom}%"); }

    // Create a snapshot — only DrawingCanvas knows what to save
    public CanvasMemento Save() => new(_shapes, _color, _zoomLevel);

    // Restore from snapshot
    public void Restore(CanvasMemento memento)
    {
        _shapes    = new List<string>(memento.Shapes);
        _color     = memento.Color;
        _zoomLevel = memento.ZoomLevel;
        Console.WriteLine($"  Restored: {_shapes.Count} shapes, color={_color}, zoom={_zoomLevel}%");
    }
}

// ── Caretaker — manages undo history ──────────────────────
class UndoManager
{
    private readonly Stack<CanvasMemento> _history = new();

    public void Push(CanvasMemento memento) => _history.Push(memento);

    public CanvasMemento? Pop() =>
        _history.TryPop(out var m) ? m : null;
}

// ── Demo ──────────────────────────────────────────────────
var canvas  = new DrawingCanvas();
var undo    = new UndoManager();

undo.Push(canvas.Save());        // snapshot 1 (empty canvas)

canvas.AddShape("Circle");
canvas.SetColor("Blue");
undo.Push(canvas.Save());        // snapshot 2

canvas.AddShape("Rectangle");
canvas.SetZoom(150);
undo.Push(canvas.Save());        // snapshot 3

Console.WriteLine("\nUndo:");
canvas.Restore(undo.Pop()!);     // back to snapshot 3 → just after Rectangle+Zoom
canvas.Restore(undo.Pop()!);     // back to snapshot 2 → Circle+Blue
canvas.Restore(undo.Pop()!);     // back to snapshot 1 → empty
```

## Key Takeaways

- The object controls what gets saved — encapsulation is preserved.
- The caretaker only stores and retrieves mementos; it never inspects their contents.
- Stack-based caretaker gives undo; two stacks (undo + redo) give undo/redo.
- Snapshots grow with object complexity — consider differential or compressed storage for large objects.

## When to Use

- You need undo/redo or time-travel debugging.
- You want to snapshot state for rollback on failure (transactional behaviour).
- Saving state externally would break encapsulation.

## When NOT to Use

- The state is trivially reconstructible from other sources (re-fetch, recalculate).
- The object's state is very large — mementos are costly; consider event sourcing instead.
