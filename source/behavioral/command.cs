// ============================================================
// Command Pattern — C# Example
// ============================================================
//
// Intent: Encapsulate a request as an object, supporting undo,
// queuing, and logging.
//
// Key roles:
//   ICommand           — Command interface (Execute / Undo)
//   TextDocument       — Receiver
//   InsertTextCommand  — Concrete Command
//   DeleteTextCommand  — Concrete Command
//   CommandHistory     — Invoker (undo stack)
// ============================================================

// ── Command interface ────────────────────────────────────
interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

// ── Receiver ────────────────────────────────────────────
class TextDocument
{
    private string _text = "";
    public string Text => _text;

    public void InsertText(string text, int position)
    {
        _text = _text.Insert(Math.Min(position, _text.Length), text);
    }

    public void DeleteText(int position, int length)
    {
        position = Math.Min(position, _text.Length);
        length = Math.Min(length, _text.Length - position);
        _text = _text.Remove(position, length);
    }
}

// ── Concrete Commands ────────────────────────────────────
class InsertTextCommand(TextDocument doc, string text, int position) : ICommand
{
    public string Description => $"Insert \"{text}\" at {position}";
    public void Execute() => doc.InsertText(text, position);
    public void Undo() => doc.DeleteText(position, text.Length);
}

class DeleteTextCommand(TextDocument doc, int position, int length) : ICommand
{
    private string _savedText = "";

    public string Description => $"Delete {length} chars at {position}";

    public void Execute()
    {
        // Save deleted text so we can restore it on Undo
        _savedText = doc.Text.Substring(
            Math.Min(position, doc.Text.Length),
            Math.Min(length, Math.Max(0, doc.Text.Length - position)));
        doc.DeleteText(position, length);
    }

    public void Undo() => doc.InsertText(_savedText, position);
}

// ── Invoker ─────────────────────────────────────────────
class CommandHistory
{
    private readonly Stack<ICommand> _history = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        _history.Push(command);
        Console.WriteLine($"  Execute: {command.Description}  → \"{GetDoc(command)}\"");
    }

    public void Undo()
    {
        if (!_history.TryPop(out var cmd))
        {
            Console.WriteLine("  Nothing to undo.");
            return;
        }
        cmd.Undo();
        Console.WriteLine($"  Undo: {cmd.Description}");
    }

    // Helper: not part of the pattern — just for demo output
    private static string GetDoc(ICommand _) => "see doc.Text below";
}

// ── Demo ─────────────────────────────────────────────────
Console.WriteLine("=== Command Pattern ===\n");

var doc = new TextDocument();
var history = new CommandHistory();

history.Execute(new InsertTextCommand(doc, "Hello", 0));
Console.WriteLine($"  Doc: \"{doc.Text}\"");

history.Execute(new InsertTextCommand(doc, " World", 5));
Console.WriteLine($"  Doc: \"{doc.Text}\"");

history.Execute(new DeleteTextCommand(doc, 5, 6));   // delete " World"
Console.WriteLine($"  Doc: \"{doc.Text}\"");

Console.WriteLine("\n--- Undo ---");
history.Undo();  // re-insert " World"
Console.WriteLine($"  Doc: \"{doc.Text}\"");

history.Undo();  // remove " World" original insert
Console.WriteLine($"  Doc: \"{doc.Text}\"");

history.Undo();  // remove "Hello"
Console.WriteLine($"  Doc: \"{doc.Text}\"");

history.Undo();  // nothing left
