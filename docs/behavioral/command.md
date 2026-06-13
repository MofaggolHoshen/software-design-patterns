# 📋 Command Pattern

The Command pattern **encapsulates a request as an object**, letting you parameterise methods with operations, queue requests, log them, and support undoable operations. It decouples the object that invokes the operation from the one that knows how to perform it.

## Intent

> Encapsulate a request as an object, thereby letting you parameterise clients with different requests, queue or log requests, and support undoable operations.

## Problem

When UI buttons, menu items, or API calls directly invoke business logic, they are tightly coupled to the implementation. Supporting undo/redo, macro recording, or request queuing becomes very difficult without a layer of abstraction around each action.

### Bad Example

```csharp
class TextEditor
{
    private string _text = "";

    // Each button calls a method directly — no undo, no logging, no queuing
    public void HandleBoldClick()    => Console.WriteLine("Applied bold — can't undo");
    public void HandleUndoClick()    => Console.WriteLine("Undo? Not implemented.");
    public void HandleCopyClick()    => Console.WriteLine("Copied — but can't replay");
}
```

### Good Example

```csharp
// ── Command interface ──────────────────────────────────────
interface ICommand
{
    void Execute();
    void Undo();
}

// ── Receiver ──────────────────────────────────────────────
class TextDocument
{
    private string _text = "";
    public string Text => _text;

    public void InsertText(string text, int position)
    {
        _text = _text.Insert(position, text);
        Console.WriteLine($"  Document: \"{_text}\"");
    }

    public void DeleteText(int position, int length)
    {
        _text = _text.Remove(position, length);
        Console.WriteLine($"  Document: \"{_text}\"");
    }
}

// ── Concrete Commands ─────────────────────────────────────
class InsertTextCommand(TextDocument doc, string text, int position) : ICommand
{
    public void Execute() => doc.InsertText(text, position);
    public void Undo()    => doc.DeleteText(position, text.Length);
}

class DeleteTextCommand(TextDocument doc, int position, int length) : ICommand
{
    private string _deletedText = "";

    public void Execute()
    {
        _deletedText = doc.Text.Substring(position, length);
        doc.DeleteText(position, length);
    }

    public void Undo() => doc.InsertText(_deletedText, position);
}

// ── Invoker ────────────────────────────────────────────────
class CommandHistory
{
    private readonly Stack<ICommand> _history = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        _history.Push(command);
    }

    public void Undo()
    {
        if (_history.TryPop(out var last))
        {
            Console.Write("  Undoing: ");
            last.Undo();
        }
    }
}

var doc     = new TextDocument();
var history = new CommandHistory();

history.Execute(new InsertTextCommand(doc, "Hello",  0));   // "Hello"
history.Execute(new InsertTextCommand(doc, " World", 5));   // "Hello World"
history.Execute(new DeleteTextCommand(doc, 5, 6));          // "Hello"

history.Undo();   // Undo delete → "Hello World"
history.Undo();   // Undo insert → "Hello"
```

## Key Takeaways

- Each command is a self-contained object — trivial to store, queue, serialize, or replay.
- Undo/redo is a natural consequence: each command stores the state needed to reverse itself.
- The invoker (`CommandHistory`) knows nothing about the domain — it just calls `Execute`/`Undo`.
- Macro recording: collect commands into a `List<ICommand>` and replay them.

## When to Use

- You need undoable operations.
- You want to queue, schedule, or log requests.
- You need to parameterise objects with actions (callbacks, menu items, toolbar buttons).

## When NOT to Use

- Operations are simple and one-shot — the overhead of a command object is not justified.
- You do not need undo, logging, or queuing — a direct method call is cleaner.
